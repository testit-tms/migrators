using AzureExporter.Client;
using AzureExporter.Models;
using AzureExporter.Services;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Constants = AzureExporter.Models.Constants;

namespace AzureExporterTests;

public class SharedStepSeriveTests
{
    private ILogger<SharedStepService> _logger;
    private IClient _client;
    private IStepService _stepService;
    private IAttachmentService _attachmentService;
    private ILinkService _linkService;
    private List<int> _workItemIds;
    private AzureWorkItem _workItem;
    private List<AzureAttachment> _azureAttachments;
    private List<Step> _steps;
    private List<string> _attachments;
    private Dictionary<string, Guid> _attributes;
    private SharedStep _sharedStep;
    private Guid _sectionId;
    private List<AzureLink> _azureLinks;
    private List<Link> _links;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<SharedStepService>>();
        _client = Substitute.For<IClient>();
        _stepService = Substitute.For<IStepService>();
        _attachmentService = Substitute.For<IAttachmentService>();
        _linkService = Substitute.For<ILinkService>();

        _sectionId = Guid.NewGuid();

        _attributes = new Dictionary<string, Guid>
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

        _azureLinks = new List<AzureLink>
        {
            new()
            {
                Title = "Test Link",
                Url = "https://www.google.com"
            }
        };

        _links = new List<Link>
        {
            new()
            {
                Title = "Test Link",
                Url = "https://www.google.com"
            }
        };

        _workItem = new AzureWorkItem
        {
            Id = 1,
            Title = "Test Case",
            Description = "Test Case Description",
            Steps = "Test Case Steps",
            Attachments = _azureAttachments,
            IterationPath = "Test Iteration Path",
            Links = _azureLinks,
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
                ActionAttachments = new List<string>(),
                SharedStepId = null
            }
        };

        _sharedStep = new SharedStep
        {
            Id = Guid.NewGuid(),
            Name = "Test Case",
            Description = "Test Case Description",
            State = StateType.NotReady,
            Priority = PriorityType.Highest,
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
    public async Task ConvertSharedSteps_FailedGetWorkItems_ReturnsEmptyList()
    {
        // Arrange
        _client.GetWorkItemIds(Constants.SharedStepType)
            .ThrowsAsync(new Exception("Failed to get work items"));

        var sharedStepService = new SharedStepService(_logger, _client, _stepService, _attachmentService, _linkService);

        // Act
        Assert.ThrowsAsync<Exception>(() =>
            sharedStepService.ConvertSharedSteps(Guid.NewGuid(),
                Guid.NewGuid(), new Dictionary<string, Guid>()));

        // Assert
        await _client.DidNotReceive()
            .GetWorkItemById(Arg.Any<int>());

        _stepService.DidNotReceive()
            .ConvertSteps(Arg.Any<string>(), Arg.Any<Dictionary<int, Guid>>());

        _linkService.DidNotReceive()
            .CovertLinks(Arg.Any<List<AzureLink>>());

        await _attachmentService.DidNotReceive()
            .DownloadAttachments(Arg.Any<List<AzureAttachment>>(), Arg.Any<Guid>());
    }

    [Test]
    public async Task ConvertSharedSteps_FailedGetWorkItemById_ReturnsEmptyList()
    {
        // Arrange
        _client.GetWorkItemIds(Constants.SharedStepType)
            .Returns(_workItemIds);

        _client.GetWorkItemById(Arg.Any<int>())
            .ThrowsAsync(new Exception("Failed to get work item by id"));

        var sharedStepService = new SharedStepService(_logger, _client, _stepService, _attachmentService, _linkService);

        // Act
        Assert.ThrowsAsync<Exception>(() =>
            sharedStepService.ConvertSharedSteps(Guid.NewGuid(),
                Guid.NewGuid(), new Dictionary<string, Guid>()));

        // Assert
        _stepService.DidNotReceive()
            .ConvertSteps(Arg.Any<string>(), Arg.Any<Dictionary<int, Guid>>());

        _linkService.DidNotReceive()
            .CovertLinks(Arg.Any<List<AzureLink>>());

        await _attachmentService.DidNotReceive()
            .DownloadAttachments(Arg.Any<List<AzureAttachment>>(), Arg.Any<Guid>());
    }

    [Test]
    public async Task ConvertSharedSteps_FailedConvertSteps_ReturnsEmptyList()
    {
        // Arrange
        _client.GetWorkItemIds(Constants.SharedStepType)
            .Returns(_workItemIds);

        _client.GetWorkItemById(1)
            .Returns(_workItem);

        _stepService.ConvertSteps(Arg.Any<string>(), Arg.Any<Dictionary<int, Guid>>())
            .Throws(new Exception("Failed to convert steps"));

        var sharedStepService = new SharedStepService(_logger, _client, _stepService, _attachmentService, _linkService);

        // Act
        Assert.ThrowsAsync<Exception>(() =>
            sharedStepService.ConvertSharedSteps(Guid.NewGuid(), _sectionId, new Dictionary<string, Guid>()));

        // Assert
        _linkService.DidNotReceive()
            .CovertLinks(Arg.Any<List<AzureLink>>());

        await _attachmentService.DidNotReceive()
            .DownloadAttachments(Arg.Any<List<AzureAttachment>>(), Arg.Any<Guid>());
    }

    [Test]
    public async Task ConvertSharedSteps_FailedDownloadAttachments_ReturnsEmptyList()
    {
        // Arrange
        _client.GetWorkItemIds(Constants.SharedStepType)
            .Returns(_workItemIds);

        _client.GetWorkItemById(1)
            .Returns(_workItem);

        _stepService.ConvertSteps(_workItem.Steps, Arg.Any<Dictionary<int, Guid>>())
            .Returns(_steps);

        _attachmentService.DownloadAttachments(Arg.Any<List<AzureAttachment>>(), Arg.Any<Guid>())
            .Throws(new Exception("Failed to download attachments"));

        var sharedStepService = new SharedStepService(_logger, _client, _stepService, _attachmentService, _linkService);

        // Act
        Assert.ThrowsAsync<Exception>(() =>
            sharedStepService.ConvertSharedSteps(Guid.NewGuid(),
                Guid.NewGuid(), new Dictionary<string, Guid>()));

        // Assert
        _linkService.DidNotReceive()
            .CovertLinks(Arg.Any<List<AzureLink>>());
    }

    [Test]
    public async Task ConvertSharedSteps_FailedConvertLinks_ReturnsEmptyList()
    {
        // Arrange
        _client.GetWorkItemIds(Constants.SharedStepType)
            .Returns(_workItemIds);

        _client.GetWorkItemById(1)
            .Returns(_workItem);

        _stepService.ConvertSteps(_workItem.Steps, Arg.Any<Dictionary<int, Guid>>())
            .Returns(_steps);

        _attachmentService.DownloadAttachments(_azureAttachments, Arg.Any<Guid>())
            .Returns(_attachments);

        _linkService.CovertLinks(Arg.Any<List<AzureLink>>())
            .Throws(new Exception("Failed to convert links"));

        var sharedStepService = new SharedStepService(_logger, _client, _stepService, _attachmentService, _linkService);

        // Act
        Assert.ThrowsAsync<Exception>(() =>
            sharedStepService.ConvertSharedSteps(Guid.NewGuid(), _sectionId, new Dictionary<string, Guid>()));
    }

    [Test]
    public async Task ConvertSharedSteps_Success_ReturnsSharedSteps()
    {
        // Arrange
        _client.GetWorkItemIds(Constants.SharedStepType)
            .Returns(_workItemIds);

        _client.GetWorkItemById(1)
            .Returns(_workItem);

        _stepService.ConvertSteps(_workItem.Steps, Arg.Any<Dictionary<int, Guid>>())
            .Returns(_steps);

        _attachmentService.DownloadAttachments(_azureAttachments, Arg.Any<Guid>())
            .Returns(_attachments);

        _linkService.CovertLinks(_azureLinks)
            .Returns(_links);

        var sharedStepService = new SharedStepService(_logger, _client, _stepService, _attachmentService, _linkService);

        // Act
        var sharedSteps = await sharedStepService.ConvertSharedSteps(Guid.NewGuid(),
            _sectionId, _attributes);

        // Assert
        Assert.That(sharedSteps, Has.Count.EqualTo(1));
        Assert.That(sharedSteps[1].Name, Is.EqualTo(_sharedStep.Name));
        Assert.That(sharedSteps[1].Description, Is.EqualTo(_sharedStep.Description));
        Assert.That(sharedSteps[1].Links[0].Title, Is.EqualTo(_sharedStep.Links[0].Title));
        Assert.That(sharedSteps[1].Links[0].Url, Is.EqualTo(_sharedStep.Links[0].Url));
        Assert.That(sharedSteps[1].State, Is.EqualTo(_sharedStep.State));
        Assert.That(sharedSteps[1].Priority, Is.EqualTo(_sharedStep.Priority));
        Assert.That(sharedSteps[1].Tags, Is.EqualTo(_sharedStep.Tags));
        Assert.That(sharedSteps[1].SectionId, Is.EqualTo(_sharedStep.SectionId));
        Assert.That(sharedSteps[1].Attributes[0].Id, Is.EqualTo(_sharedStep.Attributes[0].Id));
        Assert.That(sharedSteps[1].Attributes[0].Value, Is.EqualTo(_sharedStep.Attributes[0].Value));
        Assert.That(sharedSteps[1].Attributes[1].Id, Is.EqualTo(_sharedStep.Attributes[1].Id));
        Assert.That(sharedSteps[1].Attributes[1].Value, Is.EqualTo(_sharedStep.Attributes[1].Value));
        Assert.That(sharedSteps[1].Steps[0].Action, Is.EqualTo(_sharedStep.Steps[0].Action));
        Assert.That(sharedSteps[1].Steps[0].Expected, Is.EqualTo(_sharedStep.Steps[0].Expected));
        Assert.That(sharedSteps[1].Steps[0].ActionAttachments, Is.EqualTo(_sharedStep.Steps[0].ActionAttachments));
    }
}
