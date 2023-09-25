using AzureExporter.Client;
using AzureExporter.Models;
using AzureExporter.Services;
using JsonWriter;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AzureExporterTests;

public class AttachmentServiceTests
{
    private ILogger<AttachmentService> _logger;
    private IClient _client;
    private IWriteService _writeService;
    private List<AzureAttachment> _attachments;
    private List<string> _expectedNames;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<AttachmentService>>();
        _client = Substitute.For<IClient>();
        _writeService = Substitute.For<IWriteService>();
        _attachments = new List<AzureAttachment>();
        _expectedNames = new List<string>();

        for (var i = 0; i < 10; i++)
        {
            var name = "name" + i;

            _expectedNames.Add(name);
            _attachments.Add(
                new AzureAttachment
                {
                    Name = name,
                    Id = Guid.NewGuid()
                });
        }
    }

    [Test]
    public async Task DownloadAttachments_FailedGetAttachments()
    {
        // Arrange
        _client.GetAttachmentById(_attachments[0].Id)
            .ThrowsAsync(new Exception("Failed to download attachment"));

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.DownloadAttachments(_attachments, Guid.NewGuid()));

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
        _client.GetAttachmentById(_attachments[0].Id).Returns(bytes);
        _writeService.WriteAttachment(guid, bytes, Arg.Any<string>())
            .Throws(new Exception("Failed to write attachment"));

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.DownloadAttachments(_attachments, guid));
    }

    [Test]
    public async Task DownloadAttachments_ShouldReturnEmptyList_WhenWorkItemRelationsIsEmpty()
    {
        // Arrange
        var guid = new Guid();
        var attachmentService = new AttachmentService(_logger, _client, _writeService);

        // Act
        var result = await attachmentService.DownloadAttachments(
            new List<AzureAttachment>(),
            guid
        );

        // Assert
        Assert.That(result, Is.Empty);
    }


    [Test]
    public async Task DownloadAttachments_ShouldReturnNames_WhenWorkItemRelationsHasRelations()
    {
        // Arrange
        var guid = new Guid();
        var bytes = new byte[] { 1, 2, 3 };

        _client.GetAttachmentById(Arg.Any<Guid>())
            .Returns(bytes);

        foreach (var name in _expectedNames)
        {
            _writeService.WriteAttachment(guid, bytes, name).Returns(name);
        }

        var attachmentService = new AttachmentService(_logger, _client, _writeService);

        // Act
        var result = await attachmentService.DownloadAttachments(
            _attachments,
            guid
        );

        // Assert
        for (var i = 0; i < result.Count; i++)
        {
            Assert.That(result[i], Is.EqualTo(_expectedNames[i]));
        }
    }
}
