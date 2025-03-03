using AllureExporter.Client;
using AllureExporter.Models;
using AllureExporter.Services;
using AllureExporter.Services.Implementations;
using JsonWriter;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AllureExporterTests;

public class AttachmentServiceTests
{
    private ILogger<AttachmentService> _logger;
    private IClient _client;
    private IWriteService _writeService;
    private List<AllureAttachment> _attachments;
    private const int TestCaseId = 1;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<AttachmentService>>();
        _client = Substitute.For<IClient>();
        _writeService = Substitute.For<IWriteService>();
        _attachments = new List<AllureAttachment>
        {
            new()
            {
                Id = 1,
                Name = "TestAttachment1"
            },
            new()
            {
                Id = 2,
                Name = "TestAttachment2"
            }
        };
    }

    [Test]
    public async Task DownloadAttachments_FailedGetAttachments()
    {
        // Arrange
        _client.GetAttachmentsByTestCaseId(TestCaseId)
            .ThrowsAsync(new Exception("Failed to get attachments"));

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.DownloadAttachmentsforTestCase(TestCaseId, Guid.NewGuid()));

        // Assert
        await _client.DidNotReceive().DownloadAttachmentForTestCase(Arg.Any<int>());
        await _writeService.DidNotReceive()
            .WriteAttachment(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<string>());
    }

    [Test]
    public async Task DownloadAttachments_FailedDownloadAttachment()
    {
        // Arrange
        _client.GetAttachmentsByTestCaseId(TestCaseId).Returns(_attachments);
        _client.DownloadAttachmentForTestCase(_attachments[0].Id)
            .ThrowsAsync(new Exception("Failed to download attachment"));

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.DownloadAttachmentsforTestCase(TestCaseId, Guid.NewGuid()));

        // Assert
        await _writeService.DidNotReceive()
            .WriteAttachment(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<string>());
    }

    [Test]
    public async Task DownloadAttachments_FailedWriteAttachment()
    {
        // Arrange
        var guid = new Guid();
        var bytes = new byte[] { 1, 2, 3 };
        _client.GetAttachmentsByTestCaseId(TestCaseId).Returns(_attachments);
        _client.DownloadAttachmentForTestCase(_attachments[0].Id).Returns(bytes);
        _writeService.WriteAttachment(guid, bytes, _attachments[0].Name)
            .Throws(new Exception("Failed to write attachment"));

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.DownloadAttachmentsforTestCase(TestCaseId, guid));
    }

    [Test]
    public async Task DownloadAttachments_Success()
    {
        // Arrange
        var guid = new Guid();
        var bytes = new byte[] { 1, 2, 3 };
        _client.GetAttachmentsByTestCaseId(TestCaseId).Returns(_attachments);
        _client.DownloadAttachmentForTestCase(_attachments[0].Id).Returns(bytes);
        _client.DownloadAttachmentForTestCase(_attachments[1].Id).Returns(bytes);
        _writeService.WriteAttachment(guid, bytes, _attachments[0].Name).Returns(_attachments[0].Name);
        _writeService.WriteAttachment(guid, bytes, _attachments[1].Name).Returns(_attachments[1].Name);

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        var result = await service.DownloadAttachmentsforTestCase(TestCaseId, guid);

        // Assert
        await _writeService.Received(1).WriteAttachment(guid, bytes, _attachments[0].Name);
        await _writeService.Received(1).WriteAttachment(guid, bytes, _attachments[1].Name);
        Assert.That(result, Is.EqualTo(new List<string> { _attachments[0].Name, _attachments[1].Name }));
    }
}
