using Microsoft.Extensions.Options;
using ZephyrScaleServerExporter.Models;
using ZephyrScaleServerExporter.Services.Implementations;

using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Text.Json;
using Models;

namespace ZephyrScaleServerExporterTests.Services;

[TestFixture]
public class WriteServiceTests
{
    private Mock<ILogger<WriteService>> _mockLogger;
    private Mock<IOptions<AppConfig>> _mockAppConfig;

    private WriteService _writeService;
    private string _testDirectory;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<WriteService>>();
        _mockAppConfig = new Mock<IOptions<AppConfig>>();
        
        // Create a temporary test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _mockAppConfig.Setup(c => c.Value).Returns(new AppConfig { ResultPath = _testDirectory });
        
        _writeService = new WriteService(_mockLogger.Object, _mockAppConfig.Object);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
    
    #region Constructor

    [Test]
    public void Constructor_ThrowsExceptionIfResultPathNotSpecified()
    {
        // Arrange
        _mockAppConfig.Setup(c => c.Value).Returns(new AppConfig { ResultPath = null! });


        // Act & Assert
        Assert.That(
            () => new WriteService(_mockLogger.Object, _mockAppConfig.Object),
            Throws.ArgumentNullException.With.Message.EqualTo("Value cannot be null. (Parameter 'path')")
        );
    }

    [Test]
    public void Constructor_WithPartialExport_CreatesBatchPath()
    {
        // Arrange
        var projectKey = "TEST_PROJECT";
        var partialFolderName = "batch";
        var basePath = Path.Combine(_testDirectory, projectKey, partialFolderName);
        
        Directory.CreateDirectory(basePath + "_1");
        Directory.CreateDirectory(basePath + "_2");
        
        var config = new AppConfig
        {
            ResultPath = _testDirectory,
            Zephyr = new ZephyrConfig
            {
                ProjectKey = projectKey,
                Partial = true,
                PartialFolderName = partialFolderName
            }
        };
        _mockAppConfig.Setup(c => c.Value).Returns(config);

        // Act
        var writeService = new WriteService(_mockLogger.Object, _mockAppConfig.Object);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(writeService.GetBatchNumber(), Is.EqualTo(3));
            var expectedPath = Path.GetFullPath(basePath + "_3");
            Assert.That(Directory.Exists(expectedPath), Is.False);
        });
    }

    #endregion

    #region GetBatchNumber

    [TestCase(false, "", 0)]
    [TestCase(true, "batch", 1)]
    public void GetBatchNumber_WithDifferentConfigs_ReturnsCorrectBatchNumber(bool partial, string partialFolderName, int expectedBatchNumber)
    {
        // Arrange
        var config = new AppConfig
        {
            ResultPath = _testDirectory,
            Zephyr = new ZephyrConfig
            {
                ProjectKey = "TEST_PROJECT",
                Partial = partial,
                PartialFolderName = partialFolderName
            }
        };
        _mockAppConfig.Setup(c => c.Value).Returns(config);

        // Act
        var writeService = new WriteService(_mockLogger.Object, _mockAppConfig.Object);
        var result = writeService.GetBatchNumber();

        // Assert
        Assert.That(result, Is.EqualTo(expectedBatchNumber));
    }

    #endregion

    #region WriteAttachment

    [Test]
    public async Task WriteAttachment_WritesNewAttachmentFile()
    {
        // Arrange
        var testId = Guid.NewGuid();
        var fileName = "test.txt";
        var textContent = "Test content";
        var content = Encoding.UTF8.GetBytes(textContent);

        // Act
        var result = await _writeService.WriteAttachment(testId, content, fileName, false);

        // Assert
        var (expectedFilePath, fileContent) = GetExpectedFilePath(testId, fileName);
        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(expectedFilePath), Is.True);
            Assert.That(fileContent, Is.EqualTo(textContent));
            Assert.That(result, Is.EqualTo(fileName));
        });
    }

    [Test]
    public async Task WriteAttachment_WhenFileExists_SkipsAndReturnsFileName()
    {
        // Arrange
        var testId = Guid.NewGuid();
        var fileName = "existing.txt";
        var existingContent = "Existing content";
        var newContent = Encoding.UTF8.GetBytes("New content");

        CreateTestFile(testId, fileName, existingContent);
        var (filePath, _) = GetExpectedFilePath(testId, fileName);

        // Act
        var result = await _writeService.WriteAttachment(testId, newContent, fileName, false);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(fileName));
            Assert.That(File.ReadAllText(filePath), Is.EqualTo(existingContent));
        });
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("already exists") && v.ToString()!.Contains("skipping")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task WriteAttachment_WithSharedAttachment_StoresInAttachmentStorage()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var fileName = "shared_attachment.txt";
        var textContent = "Shared attachment content";
        var content = Encoding.UTF8.GetBytes(textContent);

        // Act
        var writeResult = await _writeService.WriteAttachment(sourceId, content, fileName, true);
        var copyResult = await _writeService.CopyAttachment(targetId, fileName);

        // Assert
        var (targetFilePath, _) = GetExpectedFilePath(targetId, fileName);
        Assert.Multiple(() =>
        {
            Assert.That(writeResult, Is.EqualTo(fileName));
            Assert.That(copyResult, Is.EqualTo(fileName));
            Assert.That(File.Exists(targetFilePath), Is.True);
            Assert.That(File.ReadAllBytes(targetFilePath), Is.EqualTo(content));
        });
    }

    #endregion

    #region CopyAttachment

    [Test]
    public async Task CopyAttachment_ReturnsNullIfSourceFileDoesNotExist()
    {
        // Arrange
        var testId = Guid.NewGuid();
        var fileName = "nonexistent.txt";

        // Act
        var result = await _writeService.CopyAttachment(testId, fileName);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CopyAttachment_WithExistingFile_CopiesSuccessfully()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var fileName = "shared_file.txt";
        var textContent = "Shared attachment content";
        var content = Encoding.UTF8.GetBytes(textContent);
        
        await _writeService.WriteAttachment(sourceId, content, fileName, true);

        // Act
        var result = await _writeService.CopyAttachment(targetId, fileName);

        // Assert
        var (targetFilePath, _) = GetExpectedFilePath(targetId, fileName);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(fileName));
            Assert.That(File.Exists(targetFilePath), Is.True);
            Assert.That(File.ReadAllBytes(targetFilePath), Is.EqualTo(content));
        });
    }

    [Test]
    public async Task CopyAttachment_WhenCalledTwice_SkipsSecondCopy()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var fileName = "duplicate_file.txt";
        var textContent = "File content";
        var content = Encoding.UTF8.GetBytes(textContent);
        
        await _writeService.WriteAttachment(sourceId, content, fileName, true);

        // Act
        var firstResult = await _writeService.CopyAttachment(targetId, fileName);
        var secondResult = await _writeService.CopyAttachment(targetId, fileName);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(firstResult, Is.EqualTo(fileName));
            Assert.That(secondResult, Is.EqualTo(fileName));
        });
    }

    #endregion

    #region IsAttachmentExist

    [Test]
    public void IsAttachmentExist_ReturnsTrueIfFileExists()
    {
        // Arrange
        var testId = Guid.NewGuid();
        var fileName = "test.txt";
        var filePath = Path.Combine(_testDirectory, testId.ToString(), fileName);

        Directory.CreateDirectory(Path.Combine(_testDirectory, testId.ToString()));
        File.WriteAllText(filePath, "Test content");

        // Act
        var result = _writeService.IsAttachmentExist(testId, fileName);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
        });
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("already exists")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void IsAttachmentExist_ReturnsFalseIfFileDoesNotExist()
    {
        // Arrange
        var testId = Guid.NewGuid();
        var fileName = "nonexistent.txt";

        // Act
        var result = _writeService.IsAttachmentExist(testId, fileName);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region WriteTestCase

    [Test]
    public async Task WriteTestCase_WritesTestCaseJsonToFile()
    {
        // Arrange
        var testCase = new global::Models.TestCase
        {
            Id = Guid.NewGuid(),
            Name = "Sample TestCase"
        };

        // Act
        await _writeService.WriteTestCase(testCase);

        // Assert
        var (expectedFilePath, deserialized) = GetExpectedFilePath<global::Models.TestCase>(testCase.Id, Constants.TestCase);
        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(expectedFilePath), Is.True);
            Assert.That(deserialized.Id, Is.EqualTo(testCase.Id));
            Assert.That(deserialized.Name, Is.EqualTo(testCase.Name));
        });
    }

    #endregion

    #region WriteSharedStep

    [Test]
    public async Task WriteSharedStep_WritesSharedStepJsonToFile()
    {
        // Arrange
        var sharedStep = new SharedStep
        {
            Id = Guid.NewGuid(),
            Name = "Sample SharedStep"
        };

        // Act
        await _writeService.WriteSharedStep(sharedStep);

        // Assert
        var (expectedFilePath, deserialized) = GetExpectedFilePath<SharedStep>(sharedStep.Id, Constants.SharedStep);
        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(expectedFilePath), Is.True);
            Assert.That(deserialized.Id, Is.EqualTo(sharedStep.Id));
            Assert.That(deserialized.Name, Is.EqualTo(sharedStep.Name));
        });
    }

    #endregion

    #region WriteMainJson

    [Test]
    public async Task WriteMainJson_WritesMainJsonToFile()
    {
        // Arrange
        var root = new Root
        {
            ProjectName = "Sample Project",
            Attributes = new List<Models.Attribute>
            {
                new() { Name = "Priority", Type = AttributeType.String },
                new() { Name = "Environment", Type = AttributeType.String }
            },
            Sections = new List<Section>
            {
                new() { Id = Guid.NewGuid(), Name = "Functional Tests" },
                new() { Id = Guid.NewGuid(), Name = "Regression Tests" }
            },
            SharedSteps = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            TestCases = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
        };

        // Act
        await _writeService.WriteMainJson(root);

        // Assert
        var (expectedFilePath, deserialized) = GetExpectedFilePath<Root>(Constants.MainJson);
        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(expectedFilePath), Is.True, "Main JSON file was not created.");
            Assert.That(deserialized.ProjectName, Is.EqualTo(root.ProjectName), "ProjectName mismatch.");
            Assert.That(deserialized.Attributes, Has.Count.EqualTo(root.Attributes.Count), "Attribute count mismatch.");
            Assert.That(deserialized.Sections, Has.Count.EqualTo(root.Sections.Count), "Section count mismatch.");
            Assert.That(deserialized.SharedSteps, Is.EquivalentTo(root.SharedSteps), "SharedSteps mismatch.");
            Assert.That(deserialized.TestCases, Is.EquivalentTo(root.TestCases), "TestCases mismatch.");
        });
    }

    #endregion

    #region Helper Methods

    private void CreateTestFile(Guid id, string fileName, string content)
    {
        var directoryPath = Path.Combine(_testDirectory, id.ToString());
        var filePath = Path.Combine(directoryPath, fileName);
        Directory.CreateDirectory(directoryPath);
        File.WriteAllText(filePath, content);
    }

    private (string filePath, string? content) GetExpectedFilePath(Guid id, string fileName)
    {
        var directoryPath = Path.Combine(_testDirectory, id.ToString());
        var filePath = Path.Combine(directoryPath, fileName);
        
        if (File.Exists(filePath))
        {
            var fileContent = File.ReadAllText(filePath);
            return (filePath, fileContent);
        }
        
        return (filePath, null);
    }

    private (string filePath, T content) GetExpectedFilePath<T>(Guid id, string fileName)
    {
        var filePath = Path.Combine(_testDirectory, id.ToString(), fileName);
        var fileContent = File.ReadAllText(filePath);
        var deserialized = JsonSerializer.Deserialize<T>(fileContent)!;
        return (filePath, deserialized);
    }

    private (string filePath, T content) GetExpectedFilePath<T>(string fileName)
    {
        var filePath = Path.Combine(_testDirectory, fileName);
        var fileContent = File.ReadAllText(filePath);
        var deserialized = JsonSerializer.Deserialize<T>(fileContent)!;
        return (filePath, deserialized);
    }

    #endregion
}

