using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using XRayExporter.Client;
using XRayExporter.Models;
using XRayExporter.Services;
using Attribute = Models.Attribute;
using Step = Models.Step;
using TestCaseData = XRayExporter.Models.TestCaseData;

namespace XRayExporterTests;

public class ExportServiceTests
{
    private ILogger<ExportService> _logger;
    private IClient _client;
    private ISectionService _sectionService;
    private ITestCaseService _testCaseService;
    private IWriteService _writeService;

    private JiraProject _jiraProject;
    private SectionData _sectionData;
    private TestCaseData _testCaseData;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<ExportService>>();
        _client = Substitute.For<IClient>();
        _sectionService = Substitute.For<ISectionService>();
        _testCaseService = Substitute.For<ITestCaseService>();
        _writeService = Substitute.For<IWriteService>();

        _jiraProject = new JiraProject
        {
            Key = "Xray",
            Name = "My project"
        };

        var sectionId = Guid.NewGuid();
        _sectionData = new SectionData
        {
            Sections = new List<Section>
            {
                new()
                {
                    Id = sectionId,
                    Name = "Section 1",
                    PreconditionSteps = new List<Step>(),
                    PostconditionSteps = new List<Step>(),
                    Sections = new List<Section>()
                }
            },
            SectionMap = new Dictionary<int, Guid>
            {
                { 1, sectionId }
            }
        };

        _testCaseData = new TestCaseData
        {
            Attributes = new List<Attribute>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Attribute 1",
                    IsActive = true,
                    IsRequired = false,
                    Type = AttributeType.String,
                    Options = new List<string>()
                }
            },
            SharedSteps = new List<SharedStep>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Shared step 1"
                }
            },
            TestCases = new List<TestCase>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Test case 1"
                }
            }
        };
    }

    [Test]
    public async Task ExportProject_FailedGetProject()
    {
        // Arrange
        _client.GetProject()
            .Throws(new Exception("Failed to get project"));

        var exportService = new ExportService(_logger, _client, _sectionService, _testCaseService, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _sectionService.DidNotReceive()
            .ConvertSections();

        await _testCaseService.DidNotReceive()
            .ConvertTestCases(Arg.Any<Dictionary<int, Guid>>());

        await _writeService.DidNotReceive()
            .WriteSharedStep(Arg.Any<SharedStep>());

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedConvertSections()
    {
        // Arrange
        _client.GetProject()
            .Returns(_jiraProject);

        _sectionService.ConvertSections()
            .Throws(new Exception("Failed to convert sections"));

        var exportService = new ExportService(_logger, _client, _sectionService, _testCaseService, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _testCaseService.DidNotReceive()
            .ConvertTestCases(Arg.Any<Dictionary<int, Guid>>());

        await _writeService.DidNotReceive()
            .WriteSharedStep(Arg.Any<SharedStep>());

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedConvertDidNotReceive()
    {
        // Arrange
        _client.GetProject()
            .Returns(_jiraProject);

        _sectionService.ConvertSections()
            .Returns(_sectionData);

        _testCaseService.ConvertTestCases(_sectionData.SectionMap)
            .Throws(new Exception("Failed to convert test cases"));

        var exportService = new ExportService(_logger, _client, _sectionService, _testCaseService, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _writeService.DidNotReceive()
            .WriteSharedStep(Arg.Any<SharedStep>());

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedWriteSharedStep()
    {
        // Arrange
        _client.GetProject()
            .Returns(_jiraProject);

        _sectionService.ConvertSections()
            .Returns(_sectionData);

        _testCaseService.ConvertTestCases(_sectionData.SectionMap)
            .Returns(_testCaseData);

        _writeService.WriteSharedStep(_testCaseData.SharedSteps[0])
            .Throws(new Exception("Failed to write shared step"));

        var exportService = new ExportService(_logger, _client, _sectionService, _testCaseService, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedWriteTestCase()
    {
        // Arrange
        _client.GetProject()
            .Returns(_jiraProject);

        _sectionService.ConvertSections()
            .Returns(_sectionData);

        _testCaseService.ConvertTestCases(_sectionData.SectionMap)
            .Returns(_testCaseData);

        _writeService.WriteTestCase(_testCaseData.TestCases[0])
            .Throws(new Exception("Failed to write test case"));

        var exportService = new ExportService(_logger, _client, _sectionService, _testCaseService, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _writeService.Received()
            .WriteSharedStep(_testCaseData.SharedSteps[0]);

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedWriteMainJson()
    {
        // Arrange
        _client.GetProject()
            .Returns(_jiraProject);

        _sectionService.ConvertSections()
            .Returns(_sectionData);

        _testCaseService.ConvertTestCases(_sectionData.SectionMap)
            .Returns(_testCaseData);

        _writeService.WriteMainJson(Arg.Any<Root>())
            .Throws(new Exception("Failed to write test case"));

        var exportService = new ExportService(_logger, _client, _sectionService, _testCaseService, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _writeService.Received()
            .WriteSharedStep(_testCaseData.SharedSteps[0]);

        await _writeService.Received()
            .WriteTestCase(_testCaseData.TestCases[0]);
    }

    [Test]
    public async Task ExportProject_Success()
    {
        // Arrange
        _client.GetProject()
            .Returns(_jiraProject);

        _sectionService.ConvertSections()
            .Returns(_sectionData);

        _testCaseService.ConvertTestCases(_sectionData.SectionMap)
            .Returns(_testCaseData);

        _writeService.WriteMainJson(Arg.Any<Root>())
            .Throws(new Exception("Failed to write test case"));

        var exportService = new ExportService(_logger, _client, _sectionService, _testCaseService, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _writeService.Received()
            .WriteSharedStep(_testCaseData.SharedSteps[0]);

        await _writeService.Received()
            .WriteTestCase(_testCaseData.TestCases[0]);

        await _writeService.Received()
            .WriteMainJson(Arg.Any<Root>());
    }
}
