using TestLinkExporter.Client;
using TestLinkExporter.Models;
using TestLinkExporter.Services;
using JsonWriter;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;

namespace TestLinkExporterTests;

public class AttachmentServiceTests
{
    private ILogger<AttachmentService> _logger;
    private IClient _client;
    private IWriteService _writeService;
    private List<TestLinkAttachment> _attachments;
    private const int TestCaseId = 1;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<AttachmentService>>();
        _client = Substitute.For<IClient>();
        _writeService = Substitute.For<IWriteService>();
        _attachments = new List<TestLinkAttachment>
        {
            new()
            {
                Content = new byte[] { 1, 2, 3 },
                Name = "TestAttachment1.png"
            },
            new()
            {
                Content = new byte[] { 1, 2, 3 },
                Name = "TestAttachment2.txt"
            }
        };
    }

    [Test]
    public async Task DownloadAttachments_FailedDownloadAttachment()
    {
        // Arrange
        _client.GetAttachmentsByTestCaseId(Arg.Any<int>())
            .Throws(new Exception("Failed to download attachment"));

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.DownloadAttachments(1, Guid.NewGuid()));

        // Assert
        _ = _writeService.DidNotReceive()
            .WriteAttachment(Guid.NewGuid(), _attachments[0].Content, _attachments[0].Name);
    }

    [Test]
    public async Task DownloadAttachments_FailedWriteAttachment()
    {
        // Arrange
        var guid = Guid.NewGuid();
        _client.GetAttachmentsByTestCaseId(1)
            .Returns(_attachments);
        _writeService.WriteAttachment(guid, _attachments[0].Content, _attachments[0].Name)
            .ThrowsAsync(new Exception("Failed to write attachment"));

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.DownloadAttachments(1, guid));
    }

    [Test]
    public async Task DownloadAttachments_Success()
    {
        // Arrange
        var guid = Guid.NewGuid();
        _client.GetAttachmentsByTestCaseId(1)
            .Returns(_attachments);
        _writeService.WriteAttachment(guid, _attachments[0].Content, _attachments[0].Name).Returns(_attachments[0].Name);
        _writeService.WriteAttachment(guid, _attachments[1].Content, _attachments[1].Name).Returns(_attachments[1].Name);

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        var result = await service.DownloadAttachments(1, guid);

        // Assert
        await _writeService.Received(1).WriteAttachment(guid, _attachments[0].Content, _attachments[0].Name);
        await _writeService.Received(1).WriteAttachment(guid, _attachments[1].Content, _attachments[1].Name);
        Assert.That(result, Is.EqualTo(new List<string> { _attachments[0].Name, _attachments[1].Name }));
    }
}

