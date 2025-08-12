using Microsoft.Extensions.Options;
using ZephyrScaleServerExporter.Models;
using ZephyrScaleServerExporter.Services.Implementations;

namespace ExporterTests;

using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Models;

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
    public async Task WriteAttachment_WritesNewAttachmentFile()
    {
        // Arrange
        var testId = Guid.NewGuid();
        var fileName = "test.txt";
        var content = Encoding.UTF8.GetBytes("Test content");

        // Act
        var result = await _writeService.WriteAttachment(testId, content, fileName, false);

        // Assert
        var expectedFilePath = Path.Combine(_testDirectory, testId.ToString(), fileName);
        Assert.That(File.Exists(expectedFilePath), Is.True);
        Assert.That(File.ReadAllText(expectedFilePath), Is.EqualTo("Test content"));
        Assert.That(result, Is.EqualTo(fileName));
    }
    
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
        Assert.That(result, Is.True);
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
    
    [Test]
    public async Task WriteTestCase_WritesTestCaseJsonToFile()
    {
        // Arrange
        var testCase = new TestCase
        {
            Id = Guid.NewGuid(),
            Name = "Sample TestCase"
        };

        // Act
        await _writeService.WriteTestCase(testCase);

        // Assert
        var expectedFilePath = Path.Combine(_testDirectory, testCase.Id.ToString(), Constants.TestCase);
        Assert.That(File.Exists(expectedFilePath), Is.True);
    
        var fileContent = await File.ReadAllTextAsync(expectedFilePath);
        var deserialized = JsonSerializer.Deserialize<TestCase>(fileContent);
        Assert.That(deserialized.Id, Is.EqualTo(testCase.Id));
        Assert.That(deserialized.Name, Is.EqualTo(testCase.Name));
    }
    
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
        var expectedFilePath = Path.Combine(_testDirectory, sharedStep.Id.ToString(), Constants.SharedStep);
        Assert.That(File.Exists(expectedFilePath), Is.True);

        var fileContent = await File.ReadAllTextAsync(expectedFilePath);
        var deserialized = JsonSerializer.Deserialize<SharedStep>(fileContent);
        Assert.That(deserialized.Id, Is.EqualTo(sharedStep.Id));
        Assert.That(deserialized.Name, Is.EqualTo(sharedStep.Name));
    }
    
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
        var expectedFilePath = Path.Combine(_testDirectory, Constants.MainJson);
        Assert.That(File.Exists(expectedFilePath), Is.True, "Main JSON file was not created.");

        var fileContent = await File.ReadAllTextAsync(expectedFilePath);
        var deserialized = JsonSerializer.Deserialize<Root>(fileContent);

        Assert.That(deserialized.ProjectName, Is.EqualTo(root.ProjectName), "ProjectName mismatch.");
        Assert.That(deserialized.Attributes, Has.Count.EqualTo(root.Attributes.Count), "Attribute count mismatch.");
        Assert.That(deserialized.Sections, Has.Count.EqualTo(root.Sections.Count), "Section count mismatch.");
        Assert.That(deserialized.SharedSteps, Is.EquivalentTo(root.SharedSteps), "SharedSteps mismatch.");
        Assert.That(deserialized.TestCases, Is.EquivalentTo(root.TestCases), "TestCases mismatch.");
    }
}

