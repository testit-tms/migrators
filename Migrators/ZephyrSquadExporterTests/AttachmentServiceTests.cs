using System.Text;
using JsonWriter;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ZephyrSquadExporter.Client;
using ZephyrSquadExporter.Models;
using ZephyrSquadExporter.Services;

namespace ZephyrSquadExporterTests;

public class AttachmentServiceTests
{
    private ILogger<AttachmentService> _logger;
    private IClient _client;
    private IWriteService _writeService;

    private const string IssueId = "ISSUE-123";
    private const string EntityId = "123";
    private readonly Guid _testCaseId = Guid.NewGuid();

    private List<ZephyrAttachment> _attachments;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<AttachmentService>>();
        _client = Substitute.For<IClient>();
        _writeService = Substitute.For<IWriteService>();

        _attachments = new List<ZephyrAttachment>
        {
            new()
            {
                Id = "123",
                Name = "test.txt",
                FileExtension = "txt"
            }
        };
    }

    [Test]
    public async Task GetAttachmentsFromExecution_FailedGetAttachmentsFromExecution()
    {
        // Arrange
        _client.GetAttachmentsFromExecution(IssueId, EntityId)
            .Throws(new Exception("Failed to get attachments from execution"));

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.GetAttachmentsFromExecution(_testCaseId, IssueId, EntityId));

        // Assert
        await _client.DidNotReceive()
            .GetAttachmentFromExecution(Arg.Any<string>(), Arg.Any<string>());

        await _writeService.DidNotReceive()
            .WriteAttachment(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<string>());
    }

    [Test]
    public async Task GetAttachmentsFromExecution_FailedGetAttachmentFromExecution()
    {
        // Arrange
        _client.GetAttachmentsFromExecution(IssueId, EntityId)
            .Returns(_attachments);

        _client.GetAttachmentFromExecution(IssueId, _attachments[0].Id)
            .Throws(new Exception("Failed to get attachment from execution"));

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.GetAttachmentsFromExecution(_testCaseId, IssueId, EntityId));

        // Assert
        await _writeService.DidNotReceive()
            .WriteAttachment(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<string>());
    }

    [Test]
    public async Task GetAttachmentsFromExecution_FailedWriteAttachment()
    {
        // Arrange
        var attachmentBytes = Encoding.UTF8.GetBytes("test");

        _client.GetAttachmentsFromExecution(IssueId, EntityId)
            .Returns(_attachments);

        _client.GetAttachmentFromExecution(IssueId, _attachments[0].Id)
            .Returns(attachmentBytes);

        _writeService.WriteAttachment(_testCaseId, attachmentBytes, _attachments[0].Name)
            .Throws(new Exception("Failed to write attachment"));

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.GetAttachmentsFromExecution(_testCaseId, IssueId, EntityId));
    }

    [Test]
    public async Task GetAttachmentsFromExecution_Success()
    {
        // Arrange
        var attachmentBytes = Encoding.UTF8.GetBytes("test");

        _client.GetAttachmentsFromExecution(IssueId, EntityId)
            .Returns(_attachments);

        _client.GetAttachmentFromExecution(IssueId, _attachments[0].Id)
            .Returns(attachmentBytes);

        _writeService.WriteAttachment(_testCaseId, attachmentBytes, _attachments[0].Name)
            .Returns(_attachments[0].Name);

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        var result = service.GetAttachmentsFromExecution(_testCaseId, IssueId, EntityId);

        // Assert
        Assert.That(result.Result[0], Is.EqualTo(_attachments[0].Name));
    }

    [Test]
    public async Task GetAttachmentsFromExecution_SuccessNoAttachments()
    {
        // Arrange
        _client.GetAttachmentsFromExecution(IssueId, EntityId)
            .Returns(new List<ZephyrAttachment>());

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        var result = service.GetAttachmentsFromExecution(_testCaseId, IssueId, EntityId);

        // Assert
        Assert.That(result.Result, Is.Empty);
    }

    [Test]
    public async Task GetAttachmentsFromStep_FailedGetAttachmentFromExecution()
    {
        // Arrange
        _client.GetAttachmentFromExecution(IssueId, _attachments[0].Id)
            .Throws(new Exception("Failed to get attachment from execution"));

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.GetAttachmentsFromStep(_testCaseId, IssueId, _attachments[0].Id, _attachments[0].Name));

        // Assert
        await _writeService.DidNotReceive()
            .WriteAttachment(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<string>());
    }

    [Test]
    public async Task GetAttachmentsFromStep_FailedWriteAttachment()
    {
        // Arrange
        var attachmentBytes = Encoding.UTF8.GetBytes("test");
        _client.GetAttachmentFromExecution(IssueId, _attachments[0].Id)
            .Returns(attachmentBytes);

        _writeService.WriteAttachment(_testCaseId, attachmentBytes, _attachments[0].Name)
            .Throws(new Exception("Failed to write attachment"));

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.GetAttachmentsFromStep(_testCaseId, IssueId, _attachments[0].Id, _attachments[0].Name));
    }

    [Test]
    public async Task GetAttachmentsFromStep_Success()
    {
        // Arrange
        var attachmentBytes = Encoding.UTF8.GetBytes("test");
        _client.GetAttachmentFromExecution(IssueId, _attachments[0].Id)
            .Returns(attachmentBytes);

        _writeService.WriteAttachment(_testCaseId, attachmentBytes, _attachments[0].Name)
            .Returns(_attachments[0].Name);

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        var results =
            await service.GetAttachmentsFromStep(_testCaseId, IssueId, _attachments[0].Id, _attachments[0].Name);

        // Assert
        Assert.That(results, Is.EqualTo(_attachments[0].Name));
    }
}
