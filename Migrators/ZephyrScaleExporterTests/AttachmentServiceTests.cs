using System.Text;
using JsonWriter;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ZephyrScaleExporter.Client;
using ZephyrScaleExporter.Models;
using ZephyrScaleExporter.Services;

namespace ZephyrScaleExporterTests;

public class AttachmentServiceTests
{
    private ILogger<AttachmentService> _logger;
    private IClient _client;
    private IWriteService _writeService;

    private const string FileName = "Test.txt";

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<AttachmentService>>();
        _client = Substitute.For<IClient>();
        _writeService = Substitute.For<IWriteService>();
    }

    [Test]
    public async Task DownloadAttachment_Success()
    {
        // Arrange
        var attachment = new ZephyrAttachment
        {
            FileName = FileName,
            Url = "https://example.com/Test.txt"
        };

        var bytes = Encoding.UTF8.GetBytes("Test");
        var guid = Guid.NewGuid();

        _client.DownloadAttachment(attachment.Url)
            .Returns(bytes);

        _writeService.WriteAttachment(guid, bytes, FileName)
            .Returns(FileName);

        var service = new AttachmentService(_logger, _writeService, _client);

        // Act
        var result = await service.DownloadAttachment(guid, attachment);

        // Assert
        Assert.That(result, Is.EqualTo(FileName));
    }

    [Test]
    public async Task DownloadAttachment_FailedWriteAttachment()
    {
        // Arrange
        var attachment = new ZephyrAttachment
        {
            FileName = FileName,
            Url = "https://example.com/Test.txt"
        };

        var bytes = Encoding.UTF8.GetBytes("Test");
        var guid = Guid.NewGuid();
        _client.DownloadAttachment(attachment.Url)
            .Returns(bytes);

        _writeService.WriteAttachment(guid, bytes, FileName)
            .Throws(new Exception("Failed to write attachment"));

        var service = new AttachmentService(_logger, _writeService, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.DownloadAttachment(guid, attachment));
    }

    [Test]
    public async Task DownloadAttachment_FailedDownloadAttachment()
    {
        // Arrange
        var attachment = new ZephyrAttachment
        {
            FileName = FileName,
            Url = "https://example.com/Test.txt"
        };

        var guid = Guid.NewGuid();
        _client.DownloadAttachment(attachment.Url)
            .Throws(new Exception("Failed to download attachment"));

        var service = new AttachmentService(_logger, _writeService, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.DownloadAttachment(guid, attachment));

        // Assert
        await _writeService.DidNotReceive()
            .WriteAttachment(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<string>());
    }
}
