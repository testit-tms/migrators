using AllureExporter.Client;
using AllureExporter.Helpers;
using AllureExporter.Services;
using AllureExporter.Services.Implementations;
using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;
using Moq;
using Attribute = Models.Attribute;

namespace AllureExporterTests;

public class ExportServiceTests
{
    private Mock<ILogger<ExportService>> _logger;
    private Mock<IClient> _client;
    private Mock<IWriteService> _writeService;
    private Mock<ISectionService> _sectionService;
    private Mock<ISharedStepService> _sharedStepService;
    private Mock<ITestCaseService> _testCaseService;
    private Mock<ICoreHelper> _coreHelper;
    private Mock<IAttributeService> _attributeService;
    private ExportService _sut;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<ExportService>>();
        _client = new Mock<IClient>();
        _writeService = new Mock<IWriteService>();
        _sectionService = new Mock<ISectionService>();
        _sharedStepService = new Mock<ISharedStepService>();
        _testCaseService = new Mock<ITestCaseService>();
        _coreHelper = new Mock<ICoreHelper>();
        _attributeService = new Mock<IAttributeService>();

        _sut = new ExportService(
            _logger.Object,
            _client.Object,
            _writeService.Object,
            _sectionService.Object,
            _sharedStepService.Object,
            _testCaseService.Object,
            _coreHelper.Object,
            _attributeService.Object);
    }

    [Test]
    public async Task ExportProject_Success()
    {
        // Arrange
        var projectId = 1L;
        var sectionId = Guid.NewGuid();
        var project = new AllureExporter.Models.Project.BaseEntity { Id = projectId, Name = "Test Project" };
        var section = new AllureExporter.Models.Project.SectionInfo
        {
            MainSection = new Section
            {
                Id = sectionId,
                Name = "Main Section"
            },
            SectionDictionary = new Dictionary<long, Guid>()
        };
        var attributes = new List<Attribute>
        {
            new() { Id = new Guid(), Name = "Priority", Type = AttributeType.Options },
            new() { Id = new Guid(), Name = "Severity", Type = AttributeType.Options }
        };
        var sharedSteps = new Dictionary<long, SharedStep>
        {
            { 1, new SharedStep { Id = Guid.NewGuid(), Name = "Shared Step 1", Steps = new List<Step>() } },
            { 2, new SharedStep { Id = Guid.NewGuid(), Name = "Shared Step 2", Steps = new List<Step>() } }
        };
        var testCases = new List<TestCase>
        {
            new() { Id = Guid.NewGuid(), Name = "Test Case 1", Steps = new List<Step>(), Priority = PriorityType.Medium, State = StateType.Ready },
            new() { Id = Guid.NewGuid(), Name = "Test Case 2", Steps = new List<Step>(), Priority = PriorityType.High, State = StateType.Ready }
        };

        _client.Setup(x => x.GetProjectId()).ReturnsAsync(project);
        _sectionService.Setup(x => x.ConvertSection(projectId)).ReturnsAsync(section);
        _attributeService.Setup(x => x.GetCustomAttributes(projectId)).ReturnsAsync(attributes);
        _sharedStepService
            .Setup(x => x.ConvertSharedSteps(projectId, section.MainSection.Id, It.IsAny<List<Attribute>>()))
            .ReturnsAsync(sharedSteps);
        _testCaseService
            .Setup(x => x.ConvertTestCases(
                projectId,
                It.IsAny<Dictionary<string, Guid>>(),
                It.IsAny<Dictionary<string, Guid>>(),
                section))
            .ReturnsAsync(testCases);

        // Act
        await _sut.ExportProject();

        // Assert
        _client.Verify(x => x.GetProjectId(), Times.Once);
        _sectionService.Verify(x => x.ConvertSection(projectId), Times.Once);
        _attributeService.Verify(x => x.GetCustomAttributes(projectId), Times.Once);
        _sharedStepService.Verify(
            x => x.ConvertSharedSteps(projectId, section.MainSection.Id, attributes),
            Times.Once);
        _testCaseService.Verify(
            x => x.ConvertTestCases(
                projectId,
                It.IsAny<Dictionary<string, Guid>>(),
                It.IsAny<Dictionary<string, Guid>>(),
                section),
            Times.Once);

        foreach (var sharedStep in sharedSteps.Values)
        {
            _coreHelper.Verify(x => x.CutLongTags(sharedStep), Times.Once);
            _writeService.Verify(x => x.WriteSharedStep(sharedStep), Times.Once);
        }

        foreach (var testCase in testCases)
        {
            _coreHelper.Verify(x => x.CutLongTags(testCase), Times.Once);
            _writeService.Verify(x => x.WriteTestCase(testCase), Times.Once);
        }

        _writeService.Verify(x => x.WriteMainJson(It.Is<Root>(r =>
            r.ProjectName == project.Name &&
            r.Sections.Count == 1 &&
            r.TestCases.Count == testCases.Count &&
            r.SharedSteps.Count == sharedSteps.Count &&
            r.Attributes.Count == attributes.Count)), Times.Once);

        _logger.VerifyLog(LogLevel.Information, "Starting export", Times.Once());
        _logger.VerifyLog(LogLevel.Information, "Ending export", Times.Once());
    }

    [Test]
    public async Task ExportProject_WhenProjectNotFound_ThrowsException()
    {
        // Arrange
        _client.Setup(x => x.GetProjectId()).ReturnsAsync((AllureExporter.Models.Project.BaseEntity)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<NullReferenceException>(() => _sut.ExportProject());
        Assert.That(ex.Message, Does.Contain("Object reference not set to an instance of an object"));
    }
}

public static class LoggerExtensions
{
    public static void VerifyLog<T>(
        this Mock<ILogger<T>> logger,
        LogLevel level,
        string message,
        Times times)
    {
        logger.Verify(x => x.Log(
            level,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains(message)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            times);
    }
}
