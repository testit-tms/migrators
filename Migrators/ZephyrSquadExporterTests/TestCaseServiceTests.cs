using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ZephyrSquadExporter.Client;
using ZephyrSquadExporter.Models;
using ZephyrSquadExporter.Services;

namespace ZephyrSquadExporterTests;

public class TestCaseServiceTests
{
    private ILogger<TestCaseService> _logger;
    private IClient _client;
    private IStepService _stepService;
    private IAttachmentService _attachmentService;
    private Dictionary<string, ZephyrSection> _sectionMap;
    private List<ZephyrExecution> _executions;
    private List<Step> _steps;
    private List<string> _attachments;


    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<TestCaseService>>();
        _client = Substitute.For<IClient>();
        _stepService = Substitute.For<IStepService>();
        _attachmentService = Substitute.For<IAttachmentService>();

        _sectionMap = new Dictionary<string, ZephyrSection>
        {
            { "1", new ZephyrSection { Id = Guid.NewGuid(), IsFolder = false, CycleId = "123" } }
        };

        _executions = new List<ZephyrExecution>
        {
            new()
            {
                Execution = new Execution
                {
                    Id = "1",
                    IssueId = 1
                },
                IssueKey = "TEST-1",
                IssueDescription = "Test description",
                IssueLabel = "Tag01, Tag02",
                IssueSummary = "Test summary"
            }
        };

        _steps = new List<Step>
        {
            new()
            {
                Action = "Test action",
                Expected = "Test expected",
                TestData = "Test data",
                ActionAttachments = new List<string>
                {
                    "Test attachment 1"
                },
                ExpectedAttachments = new List<string>(),
                TestDataAttachments = new List<string>()
            }
        };

        _attachments = new List<string>
        {
            "Test attachment 2"
        };
    }

    [Test]
    public async Task ConvertTestCases_FailedGetTestCasesFromCycle()
    {
        // Arrange
        _client.GetTestCasesFromCycle("1")
            .Throws(new Exception("Failed to get test cases from cycle"));

        var testCaseService = new TestCaseService(_logger, _client, _stepService, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await testCaseService.ConvertTestCases(_sectionMap));

        // Assert
        await _client.DidNotReceive()
            .GetTestCasesFromFolder(Arg.Any<string>(), Arg.Any<string>());

        await _stepService.DidNotReceive()
            .ConvertSteps(Arg.Any<Guid>(), Arg.Any<string>());

        await _attachmentService.DidNotReceive()
            .GetAttachmentsFromExecution(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task ConvertTestCases_FailedGetTestCasesFromFolder()
    {
        // Arrange
        _client.GetTestCasesFromFolder("123", "1")
            .Throws(new Exception("Failed to get test cases from folder"));

        var testCaseService = new TestCaseService(_logger, _client, _stepService, _attachmentService);
        _sectionMap["1"].IsFolder = true;

        // Act
        Assert.ThrowsAsync<Exception>(async () => await testCaseService.ConvertTestCases(_sectionMap));

        // Assert
        await _client.DidNotReceive()
            .GetTestCasesFromCycle(Arg.Any<string>());

        await _stepService.DidNotReceive()
            .ConvertSteps(Arg.Any<Guid>(), Arg.Any<string>());

        await _attachmentService.DidNotReceive()
            .GetAttachmentsFromExecution(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task ConvertTestCases_FailedConvertSteps()
    {
        // Arrange
        _client.GetTestCasesFromCycle("1")
            .Returns(_executions);

        _stepService.ConvertSteps(Arg.Any<Guid>(), _executions[0].Execution.IssueId.ToString())
            .Throws(new Exception("Failed to convert steps"));

        var testCaseService = new TestCaseService(_logger, _client, _stepService, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await testCaseService.ConvertTestCases(_sectionMap));

        // Assert
        await _client.DidNotReceive()
            .GetTestCasesFromFolder(Arg.Any<string>(), Arg.Any<string>());

        await _attachmentService.DidNotReceive()
            .GetAttachmentsFromExecution(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task ConvertTestCases_FailedGetAttachmentsFromExecution()
    {
        // Arrange
        _client.GetTestCasesFromCycle("1")
            .Returns(_executions);

        _stepService.ConvertSteps(Arg.Any<Guid>(), _executions[0].Execution.IssueId.ToString())
            .Returns(_steps);

        _attachmentService.GetAttachmentsFromExecution(Arg.Any<Guid>(),
                _executions[0].Execution.IssueId.ToString(),
                _executions[0].Execution.Id)
            .Throws(new Exception("Failed to get attachments from execution"));

        var testCaseService = new TestCaseService(_logger, _client, _stepService, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await testCaseService.ConvertTestCases(_sectionMap));

        // Assert
        await _client.DidNotReceive()
            .GetTestCasesFromFolder(Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task ConvertTestCases_Success()
    {
        // Arrange
        _client.GetTestCasesFromCycle("1")
            .Returns(_executions);

        _stepService.ConvertSteps(Arg.Any<Guid>(), _executions[0].Execution.IssueId.ToString())
            .Returns(_steps);

        _attachmentService.GetAttachmentsFromExecution(Arg.Any<Guid>(),
                _executions[0].Execution.IssueId.ToString(),
                _executions[0].Execution.Id)
            .Returns(_attachments);

        var testCaseService = new TestCaseService(_logger, _client, _stepService, _attachmentService);

        // Act
        var result = await testCaseService.ConvertTestCases(_sectionMap);

        // Assert
        await _client.DidNotReceive()
            .GetTestCasesFromFolder(Arg.Any<string>(), Arg.Any<string>());

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo(_executions[0].IssueKey));
        Assert.That(result[0].Description, Is.EqualTo(_executions[0].IssueDescription));
        Assert.That(result[0].State, Is.EqualTo(StateType.NotReady));
        Assert.That(result[0].Priority, Is.EqualTo(PriorityType.Medium));
        Assert.That(result[0].Steps, Has.Count.EqualTo(1));
        Assert.That(result[0].Steps[0].Action, Is.EqualTo(_steps[0].Action));
        Assert.That(result[0].Steps[0].Expected, Is.EqualTo(_steps[0].Expected));
        Assert.That(result[0].Steps[0].TestData, Is.EqualTo(_steps[0].TestData));
        Assert.That(result[0].Steps[0].ActionAttachments, Has.Count.EqualTo(1));
        Assert.That(result[0].Steps[0].ExpectedAttachments, Has.Count.EqualTo(0));
        Assert.That(result[0].Steps[0].TestDataAttachments, Has.Count.EqualTo(0));
        Assert.That(result[0].Steps[0].ActionAttachments[0], Is.EqualTo(_steps[0].ActionAttachments[0]));
        Assert.That(result[0].Attachments, Has.Count.EqualTo(2));
        Assert.That(result[0].Attachments[0], Is.EqualTo(_attachments[0]));
        Assert.That(result[0].Attachments[1], Is.EqualTo(_steps[0].ActionAttachments[0]));
        Assert.That(result[0].Tags, Has.Count.EqualTo(2));
        Assert.That(result[0].Tags[0], Is.EqualTo("Tag01"));
        Assert.That(result[0].Tags[1], Is.EqualTo("Tag02"));
    }
}
