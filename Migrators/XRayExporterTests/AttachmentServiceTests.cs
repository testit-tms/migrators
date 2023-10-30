using System.Text;
using JsonWriter;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using XRayExporter.Client;
using XRayExporter.Services;

namespace XRayExporterTests;

public class AttachmentServiceTests
{
    private ILogger<AttachmentService> _logger;
    private IClient _client;
    private IWriteService _writeService;

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
        var bytes = Encoding.UTF8.GetBytes("Test");
        var guid = Guid.NewGuid();

        _client.DownloadAttachment(Arg.Any<string>())
            .Returns(bytes);

        _writeService.WriteAttachment(guid, bytes, Arg.Any<string>())
            .Returns("Test.txt");

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        var result = await service.DownloadAttachment(guid, "https://example.com/Test.txt", "Test.txt");

        // Assert
        Assert.That(result, Is.EqualTo("Test.txt"));
    }

    [Test]
    public async Task DownloadAttachment_FailedDownloadAttachment()
    {
        // Arrange
        var guid = Guid.NewGuid();

        _client.DownloadAttachment(Arg.Any<string>())
            .Throws(new Exception("Failed to download attachment"));

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.DownloadAttachment(guid, "https://example.com/Test.txt", "Test.txt"));

        // Assert
        await _writeService.DidNotReceive()
            .WriteAttachment(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<string>());
    }

    [Test]
    public async Task DownloadAttachment_FailedWriteAttachment()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes("Test");
        var guid = Guid.NewGuid();

        _client.DownloadAttachment(Arg.Any<string>())
            .Returns(bytes);

        _writeService.WriteAttachment(guid, bytes, Arg.Any<string>())
            .Throws(new Exception("Failed to write attachment"));

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.DownloadAttachment(guid, "https://example.com/Test.txt", "Test.txt"));
    }
}
