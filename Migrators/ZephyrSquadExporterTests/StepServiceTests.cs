using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ZephyrSquadExporter.Client;
using ZephyrSquadExporter.Models;
using ZephyrSquadExporter.Services;

namespace ZephyrSquadExporterTests;

public class StepServiceTests
{
    private ILogger<StepService> _logger;
    private IClient _client;
    private IAttachmentService _attachmentService;

    private const string IssueId = "12345";
    private readonly Guid _testCaseId = Guid.NewGuid();

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<StepService>>();
        _client = Substitute.For<IClient>();
        _attachmentService = Substitute.For<IAttachmentService>();
    }

    [Test]
    public async Task ConvertSteps_FailedGetSteps()
    {
        // Arrange
        _client.GetSteps(IssueId)
            .Throws(new Exception("Failed to get steps"));

        var stepService = new StepService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await stepService.ConvertSteps(_testCaseId, IssueId));

        // Assert
        await _attachmentService.DidNotReceive()
            .GetAttachmentsFromStep(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>()
            );
    }

    [Test]
    public async Task ConvertSteps_FailedGetAttachmentsFromStep()
    {
        // Arrange
        var steps = new List<ZephyrStep>
        {
            new()
            {
                Id = "1",
                Step = "Step 1",
                Result = "Result 1",
                Data = "Data 1",
                Attachments = new List<ZephyrAttachment>
                {
                    new()
                    {
                        Id = "1",
                        Name = "Attachment 1"
                    }
                }
            }
        };

        _client.GetSteps(IssueId)
            .Returns(steps);

        _attachmentService.GetAttachmentsFromStep(_testCaseId, IssueId, "1", "Attachment 1")
            .Throws(new Exception("Failed to get attachment"));

        var stepService = new StepService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await stepService.ConvertSteps(_testCaseId, IssueId));
    }

    [Test]
    public async Task ConvertSteps_Success()
    {
        // Arrange
        var steps = new List<ZephyrStep>
        {
            new()
            {
                Id = "1",
                Step = "Step 1",
                Result = "Result 1",
                Data = "Data 1",
                Attachments = new List<ZephyrAttachment>
                {
                    new()
                    {
                        Id = "1",
                        Name = "Attachment 1.txt"
                    }
                }
            }
        };

        _client.GetSteps(IssueId)
            .Returns(steps);

        _attachmentService.GetAttachmentsFromStep(_testCaseId, IssueId, steps[0].Attachments[0].Id,
                steps[0].Attachments[0].Name)
            .Returns(steps[0].Attachments[0].Name);

        var stepService = new StepService(_logger, _client, _attachmentService);

        // Act
        var result = await stepService.ConvertSteps(_testCaseId, IssueId);

        // Assert
        Assert.That(result[0].Action, Is.EqualTo(steps[0].Step));
        Assert.That(result[0].Expected, Is.EqualTo(steps[0].Result));
        Assert.That(result[0].TestData,
            Is.EqualTo(steps[0].Data + $"<p><p><<<{steps[0].Attachments[0].Name}>>></p></p>"));
        Assert.That(result[0].TestDataAttachments[0], Is.EqualTo(steps[0].Attachments[0].Name));
    }
}
