using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Models;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models.Attributes;
using ZephyrScaleServerExporter.Models.Client;
using ZephyrScaleServerExporter.Models.Common;
using ZephyrScaleServerExporter.Models.TestCases;
using ZephyrScaleServerExporter.Services;
using ZephyrScaleServerExporter.Services.Implementations;
using TestCaseDataModel = ZephyrScaleServerExporter.Models.TestCases.TestCaseData;

namespace ZephyrScaleServerExporterTests.Services;

/// <summary>
/// Test suite for ExportService functionality
/// </summary>
[TestFixture]
public class ExportServiceTests
{
    private Mock<ILogger<ExportService>> _mockLogger;
    private Mock<IClient> _mockClient;
    private Mock<IFolderService> _mockFolderService;
    private Mock<IAttributeService> _mockAttributeService;
    private Mock<ITestCaseService> _mockTestCaseService;
    private Mock<ITestCaseBatchService> _mockTestCaseBatchService;
    private Mock<IWriteService> _mockWriteService;
    private ExportService _exportService;

    private ZephyrProject _testProject;
    private SectionData _testFolders;
    private AttributeData _testAttributes;
    private TestCaseDataModel _testTestCaseData;

    [SetUp]
    public void SetUp()
    {
        // Arrange - initialize mocks and service
        _mockLogger = new Mock<ILogger<ExportService>>();
        _mockClient = new Mock<IClient>();
        _mockFolderService = new Mock<IFolderService>();
        _mockAttributeService = new Mock<IAttributeService>();
        _mockTestCaseService = new Mock<ITestCaseService>();
        _mockTestCaseBatchService = new Mock<ITestCaseBatchService>();
        _mockWriteService = new Mock<IWriteService>();

        // Initialize common test data
        _testProject = new ZephyrProject
        {
            Id = "PROJECT-123",
            Key = "PROJ",
            Name = "Main Section"
        };

        var mainSection = new Section
        {
            Id = Guid.NewGuid(),
            Name = "Main Section"
        };

        _testFolders = new SectionData
        {
            MainSection = mainSection,
            SectionMap = new Dictionary<string, Guid> { { "Main Section", mainSection.Id } },
            AllSections = new Dictionary<string, Section> { { "Main Section", mainSection } }
        };

        var priorityAttribute = new Models.Attribute
        {
            Id = Guid.NewGuid(),
            Name = "Priority",
            Type = AttributeType.Options,
            Options = new List<string> { "High", "Medium", "Low" }
        };

        _testAttributes = new AttributeData
        {
            Attributes = new List<Models.Attribute> { priorityAttribute },
            AttributeMap = new Dictionary<string, Models.Attribute> { { "Priority", priorityAttribute } }
        };

        _testTestCaseData = new TestCaseDataModel
        {
            TestCaseIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            Attributes = new List<Models.Attribute> { priorityAttribute }
        };

        // Create service instance
        _exportService = new ExportService(
            _mockLogger.Object,
            _mockClient.Object,
            _mockFolderService.Object,
            _mockAttributeService.Object,
            _mockTestCaseService.Object,
            _mockTestCaseBatchService.Object,
            _mockWriteService.Object);

        // Setup common mock behaviors for happy path scenarios
        _mockClient.Setup(x => x.GetProject()).ReturnsAsync(_testProject);
        _mockFolderService.Setup(x => x.ConvertSections(_testProject.Name)).Returns(_testFolders);
        _mockAttributeService.Setup(x => x.ConvertAttributes(_testProject.Id)).ReturnsAsync(_testAttributes);
        _mockTestCaseService.Setup(x => x.ExportTestCases(_testFolders, _testAttributes.AttributeMap, _testProject.Id))
            .ReturnsAsync(_testTestCaseData);
        _mockTestCaseBatchService.Setup(x => x.ExportTestCasesBatch(_testFolders, _testAttributes.AttributeMap, _testProject.Id))
            .ReturnsAsync(_testTestCaseData);
        _mockWriteService.Setup(x => x.WriteMainJson(It.IsAny<Root>())).Returns(Task.CompletedTask);
    }

    #region ExportProject

    [Test]
    public async Task ExportProject_NormalExecutionFlow_CallsAllDependencies()
    {
        // Act
        await _exportService.ExportProject();

        // Assert
        _mockClient.Verify(x => x.GetProject(), Times.Once);
        _mockFolderService.Verify(x => x.ConvertSections(_testProject.Name), Times.Once);
        _mockAttributeService.Verify(x => x.ConvertAttributes(_testProject.Id), Times.Once);
        _mockTestCaseService.Verify(x => x.ExportTestCases(_testFolders, _testAttributes.AttributeMap, _testProject.Id), Times.Once);
        _mockWriteService.Verify(x => x.WriteMainJson(It.IsAny<Root>()), Times.Once);

        // Verify logging of start and completion
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exporting project")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Export complete")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ExportProject_NormalExecutionFlow_CreatesCorrectRootObject()
    {
        // Arrange
        Root? capturedRoot = null;
        _mockWriteService.Setup(x => x.WriteMainJson(It.IsAny<Root>()))
            .Callback<Root>(r => capturedRoot = r)
            .Returns(Task.CompletedTask);

        // Act
        await _exportService.ExportProject();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(capturedRoot, Is.Not.Null, "Root object should be created");
            Assert.That(capturedRoot!.ProjectName, Is.EqualTo(_testProject.Name), "Project name should match");
            Assert.That(capturedRoot.Attributes, Is.EqualTo(_testTestCaseData.Attributes), "Attributes should match");
            Assert.That(capturedRoot.Sections, Has.Count.EqualTo(1), "Should contain one section");
            Assert.That(capturedRoot.Sections[0], Is.EqualTo(_testFolders.MainSection), "Main section should match");
            Assert.That(capturedRoot.SharedSteps, Is.Empty, "Shared steps should be empty");
            Assert.That(capturedRoot.TestCases, Is.EqualTo(_testTestCaseData.TestCaseIds), "Test case IDs should match");
        });
    }

    [Test]
    public void ExportProject_WhenClientThrowsException_ThrowsException()
    {
        // Arrange - override the default setup from SetUp
        _mockClient.Setup(x => x.GetProject()).ThrowsAsync(new InvalidOperationException("Network error"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _exportService.ExportProject());
        Assert.That(ex.Message, Is.EqualTo("Network error"));
    }

    [Test]
    public void ExportProject_WhenFolderServiceThrowsException_ThrowsException()
    {
        // Arrange - override the default setup from SetUp
        _mockFolderService.Setup(x => x.ConvertSections(_testProject.Name)).Throws(new ArgumentException("Invalid project name"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _exportService.ExportProject());
        Assert.That(ex.Message, Is.EqualTo("Invalid project name"));
    }

    [Test]
    public void ExportProject_WhenAttributeServiceThrowsException_ThrowsException()
    {
        // Arrange - override the default setup from SetUp
        _mockAttributeService.Setup(x => x.ConvertAttributes(_testProject.Id)).ThrowsAsync(new InvalidOperationException("API error"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _exportService.ExportProject());
        Assert.That(ex.Message, Is.EqualTo("API error"));
    }

    [Test]
    public void ExportProject_WhenTestCaseServiceThrowsException_ThrowsException()
    {
        // Arrange - override the default setup from SetUp
        _mockTestCaseService.Setup(x => x.ExportTestCases(_testFolders, _testAttributes.AttributeMap, _testProject.Id))
            .ThrowsAsync(new InvalidOperationException("Export failed"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _exportService.ExportProject());
        Assert.That(ex.Message, Is.EqualTo("Export failed"));
    }

    [Test]
    public void ExportProject_WhenWriteServiceThrowsException_ThrowsException()
    {
        // Arrange - override the default setup from SetUp
        _mockWriteService.Setup(x => x.WriteMainJson(It.IsAny<Root>())).ThrowsAsync(new IOException("File write error"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<IOException>(async () => await _exportService.ExportProject());
        Assert.That(ex.Message, Is.EqualTo("File write error"));
    }

    #endregion

    #region ExportProjectBatch

    [Test]
    public async Task ExportProjectBatch_NormalExecutionFlow_CallsAllDependencies()
    {
        // Act
        await _exportService.ExportProjectBatch();

        // Assert
        _mockClient.Verify(x => x.GetProject(), Times.Once);
        _mockFolderService.Verify(x => x.ConvertSections(_testProject.Name), Times.Once);
        _mockAttributeService.Verify(x => x.ConvertAttributes(_testProject.Id), Times.Once);
        _mockTestCaseBatchService.Verify(x => x.ExportTestCasesBatch(_testFolders, _testAttributes.AttributeMap, _testProject.Id), Times.Once);
        _mockWriteService.Verify(x => x.WriteMainJson(It.IsAny<Root>()), Times.Once);

        // Verify logging of start and completion
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exporting project batch")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Export complete")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ExportProjectBatch_NormalExecutionFlow_CreatesCorrectRootObject()
    {
        // Arrange
        Root? capturedRoot = null;
        _mockWriteService.Setup(x => x.WriteMainJson(It.IsAny<Root>()))
            .Callback<Root>(r => capturedRoot = r)
            .Returns(Task.CompletedTask);

        // Act
        await _exportService.ExportProjectBatch();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(capturedRoot, Is.Not.Null, "Root object should be created");
            Assert.That(capturedRoot!.ProjectName, Is.EqualTo(_testProject.Name), "Project name should match");
            Assert.That(capturedRoot.Attributes, Is.EqualTo(_testTestCaseData.Attributes), "Attributes should match");
            Assert.That(capturedRoot.Sections, Has.Count.EqualTo(1), "Should contain one section");
            Assert.That(capturedRoot.Sections[0], Is.EqualTo(_testFolders.MainSection), "Main section should match");
            Assert.That(capturedRoot.SharedSteps, Is.Empty, "Shared steps should be empty");
            Assert.That(capturedRoot.TestCases, Is.EqualTo(_testTestCaseData.TestCaseIds), "Test case IDs should match");
        });
    }

    [Test]
    public void ExportProjectBatch_WhenClientThrowsException_ThrowsException()
    {
        // Arrange - override the default setup from SetUp
        _mockClient.Setup(x => x.GetProject()).ThrowsAsync(new InvalidOperationException("Network error"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _exportService.ExportProjectBatch());
        Assert.That(ex.Message, Is.EqualTo("Network error"));
    }

    [Test]
    public void ExportProjectBatch_WhenFolderServiceThrowsException_ThrowsException()
    {
        // Arrange - override the default setup from SetUp
        _mockFolderService.Setup(x => x.ConvertSections(_testProject.Name)).Throws(new ArgumentException("Invalid project name"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _exportService.ExportProjectBatch());
        Assert.That(ex.Message, Is.EqualTo("Invalid project name"));
    }

    [Test]
    public void ExportProjectBatch_WhenAttributeServiceThrowsException_ThrowsException()
    {
        // Arrange - override the default setup from SetUp
        _mockAttributeService.Setup(x => x.ConvertAttributes(_testProject.Id)).ThrowsAsync(new InvalidOperationException("API error"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _exportService.ExportProjectBatch());
        Assert.That(ex.Message, Is.EqualTo("API error"));
    }

    [Test]
    public void ExportProjectBatch_WhenTestCaseBatchServiceThrowsException_ThrowsException()
    {
        // Arrange - override the default setup from SetUp
        _mockTestCaseBatchService.Setup(x => x.ExportTestCasesBatch(_testFolders, _testAttributes.AttributeMap, _testProject.Id))
            .ThrowsAsync(new InvalidOperationException("Batch export failed"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _exportService.ExportProjectBatch());
        Assert.That(ex.Message, Is.EqualTo("Batch export failed"));
    }

    [Test]
    public void ExportProjectBatch_WhenWriteServiceThrowsException_ThrowsException()
    {
        // Arrange - override the default setup from SetUp
        _mockWriteService.Setup(x => x.WriteMainJson(It.IsAny<Root>())).ThrowsAsync(new IOException("File write error"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<IOException>(async () => await _exportService.ExportProjectBatch());
        Assert.That(ex.Message, Is.EqualTo("File write error"));
    }

    #endregion
}
