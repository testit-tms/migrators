using AzureExporter.Client;
using AzureExporter.Models;
using AzureExporter.Services;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Constants = AzureExporter.Models.Constants;

namespace AzureExporterTests;

public class TestCaseServiceTests
{
    private ILogger<TestCaseService> _logger;
    private IClient _client;
    private IStepService _stepService;
    private IAttachmentService _attachmentService;
    private List<int> _workItemIds;
    private AzureWorkItem _workItem;
    private List<AzureAttachment> _azureAttachments;
    private List<Step> _steps;
    private List<string> _attachments;
    private Dictionary<string, Guid> _attributes;
    private TestCase _testCase;
    private Guid _sectionId;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<TestCaseService>>();
        _client = Substitute.For<IClient>();
        _stepService = Substitute.For<IStepService>();
        _attachmentService = Substitute.For<IAttachmentService>();

        _sectionId = Guid.NewGuid();

        _attributes = new Dictionary<string, Guid>()
        {
            { Constants.IterationAttributeName, Guid.NewGuid() },
            { Constants.StateAttributeName, Guid.NewGuid() }
        };

        _workItemIds = new List<int>
        {
            1
        };

        _azureAttachments = new List<AzureAttachment>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Test Attachment",
            }
        };

        _attachments = new List<string>
        {
            "Test Attachment"
        };

        _workItem = new AzureWorkItem
        {
            Id = 1,
            Title = "Test Case",
            Description = "Test Case Description",
            Steps = "Test Case Steps",
            Attachments = _azureAttachments,
            IterationPath = "Test Iteration Path",
            Links = new List<AzureLink>
            {
                new()
                {
                    Title = "Test Link",
                    Url = "https://www.google.com"
                }
            },
            State = "Active",
            Priority = 1,
            Tags = "Test Tag"
        };

        _steps = new List<Step>
        {
            new()
            {
                Action = "Test Action",
                Expected = "Test Expected",
                Attachments = new List<string>(),
                SharedStepId = null
            }
        };

        _testCase = new TestCase
        {
            Id = Guid.NewGuid(),
            Name = "Test Case",
            Description = "Test Case Description",
            State = StateType.Ready,
            Priority = PriorityType.Highest,
            PreconditionSteps = new List<Step>(),
            PostconditionSteps = new List<Step>(),
            Duration = 10,
            Attributes = new List<CaseAttribute>
            {
                new()
                {
                    Id = _attributes[Constants.IterationAttributeName],
                    Value = "Test Iteration Path"
                },
                new()
                {
                    Id = _attributes[Constants.StateAttributeName],
                    Value = "Active"
                }
            },
            Steps = _steps,
            Attachments = _attachments,
            Tags = new List<string>
            {
                "Test Tag"
            },
            Iterations = new List<Iteration>(),
            SectionId = _sectionId,
            Links = new List<Link>
            {
                new()
                {
                    Title = "Test Link",
                    Url = "https://www.google.com"
                }
            },
        };
    }

    [Test]
    public async Task ConvertTestCases_FailedGetWorkItems_ReturnsEmptyList()
    {
        // Arrange
        _client.GetWorkItemIds(Constants.TestCaseType)
            .ThrowsAsync(new Exception("Failed to get work items"));

        var testCaseService = new TestCaseService(_logger, _client, _stepService, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(() =>
            testCaseService.ConvertTestCases(Guid.NewGuid(), new Dictionary<int, Guid>(),
                Guid.NewGuid(), new Dictionary<string, Guid>()));

        // Assert
        await _client.DidNotReceive()
            .GetWorkItemById(Arg.Any<int>());

        _stepService.DidNotReceive()
            .ConvertSteps(Arg.Any<string>(), Arg.Any<Dictionary<int, Guid>>());

        await _attachmentService.DidNotReceive()
            .DownloadAttachments(Arg.Any<List<AzureAttachment>>(), Arg.Any<Guid>());
    }

    [Test]
    public async Task ConvertTestCases_FailedGetWorkItemById_ReturnsEmptyList()
    {
        // Arrange
        _client.GetWorkItemIds(Constants.TestCaseType)
            .Returns(_workItemIds);

        _client.GetWorkItemById(Arg.Any<int>())
            .ThrowsAsync(new Exception("Failed to get work item by id"));

        var testCaseService = new TestCaseService(_logger, _client, _stepService, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(() =>
            testCaseService.ConvertTestCases(Guid.NewGuid(), new Dictionary<int, Guid>(),
                Guid.NewGuid(), new Dictionary<string, Guid>()));

        // Assert
        _stepService.DidNotReceive()
            .ConvertSteps(Arg.Any<string>(), Arg.Any<Dictionary<int, Guid>>());

        await _attachmentService.DidNotReceive()
            .DownloadAttachments(Arg.Any<List<AzureAttachment>>(), Arg.Any<Guid>());
    }

    [Test]
    public async Task ConvertTestCases_FailedConvertSteps_ReturnsEmptyList()
    {
        // Arrange
        _client.GetWorkItemIds(Constants.TestCaseType)
            .Returns(_workItemIds);

        _client.GetWorkItemById(1)
            .Returns(_workItem);

        _stepService.ConvertSteps(Arg.Any<string>(), Arg.Any<Dictionary<int, Guid>>())
            .Throws(new Exception("Failed to convert steps"));

        var testCaseService = new TestCaseService(_logger, _client, _stepService, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(() =>
            testCaseService.ConvertTestCases(Guid.NewGuid(), new Dictionary<int, Guid>(),
                Guid.NewGuid(), new Dictionary<string, Guid>()));

        // Assert
        await _attachmentService.DidNotReceive()
            .DownloadAttachments(Arg.Any<List<AzureAttachment>>(), Arg.Any<Guid>());
    }

    [Test]
    public async Task ConvertTestCases_FailedDownloadAttachments_ReturnsEmptyList()
    {
        // Arrange
        _client.GetWorkItemIds(Constants.TestCaseType)
            .Returns(_workItemIds);

        _client.GetWorkItemById(1)
            .Returns(_workItem);

        _stepService.ConvertSteps(_workItem.Steps, Arg.Any<Dictionary<int, Guid>>())
            .Returns(_steps);

        _attachmentService.DownloadAttachments(Arg.Any<List<AzureAttachment>>(), Arg.Any<Guid>())
            .Throws(new Exception("Failed to download attachments"));

        var testCaseService = new TestCaseService(_logger, _client, _stepService, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(() =>
            testCaseService.ConvertTestCases(Guid.NewGuid(), new Dictionary<int, Guid>(),
                Guid.NewGuid(), new Dictionary<string, Guid>()));
    }

    [Test]
    public async Task ConvertTestCases_Success_ReturnsTestCases()
    {
        // Arrange
        _client.GetWorkItemIds(Constants.TestCaseType)
            .Returns(_workItemIds);

        _client.GetWorkItemById(1)
            .Returns(_workItem);

        _stepService.ConvertSteps(_workItem.Steps, Arg.Any<Dictionary<int, Guid>>())
            .Returns(_steps);

        _attachmentService.DownloadAttachments(_azureAttachments, Arg.Any<Guid>())
            .Returns(_attachments);

        var testCaseService = new TestCaseService(_logger, _client, _stepService, _attachmentService);

        // Act
        var testCases = await testCaseService.ConvertTestCases(Guid.NewGuid(), new Dictionary<int, Guid>(),
            _sectionId, _attributes);

        // Assert
        Assert.That(testCases, Has.Count.EqualTo(1));
        Assert.That(testCases[0].Name, Is.EqualTo(_testCase.Name));
        Assert.That(testCases[0].Description, Is.EqualTo(_testCase.Description));
        // Assert.That(testCases[0].Links[0].Title, Is.EqualTo(_testCase.Links[0].Title));
        // Assert.That(testCases[0].Links[0].Url, Is.EqualTo(_testCase.Links[0].Url));
        Assert.That(testCases[0].State, Is.EqualTo(_testCase.State));
        Assert.That(testCases[0].Priority, Is.EqualTo(_testCase.Priority));
        Assert.That(testCases[0].Tags, Is.EqualTo(_testCase.Tags));
        Assert.That(testCases[0].Iterations, Is.EqualTo(_testCase.Iterations));
        Assert.That(testCases[0].SectionId, Is.EqualTo(_testCase.SectionId));
        Assert.That(testCases[0].Attributes[0].Id, Is.EqualTo(_testCase.Attributes[0].Id));
        Assert.That(testCases[0].Attributes[0].Value, Is.EqualTo(_testCase.Attributes[0].Value));
        Assert.That(testCases[0].Attributes[1].Id, Is.EqualTo(_testCase.Attributes[1].Id));
        Assert.That(testCases[0].Attributes[1].Value, Is.EqualTo(_testCase.Attributes[1].Value));
        Assert.That(testCases[0].Steps[0].Action, Is.EqualTo(_testCase.Steps[0].Action));
        Assert.That(testCases[0].Steps[0].Expected, Is.EqualTo(_testCase.Steps[0].Expected));
        Assert.That(testCases[0].Steps[0].Attachments, Is.EqualTo(_testCase.Steps[0].Attachments));
    }
}
