using System.Text;
using JsonWriter;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SpiraTestExporter.Client;
using SpiraTestExporter.Models;
using SpiraTestExporter.Services;

namespace SpiraTestExporterTests;

public class AttachmentServiceTests
{
    private ILogger<AttachmentService> _logger;
    private IClient _client;
    private IWriteService _writeService;

    private Guid _testCaseId = Guid.NewGuid();
    private const int ProjectId = 1;
    private const int ArtifactId = 12;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<AttachmentService>>();
        _client = Substitute.For<IClient>();
        _writeService = Substitute.For<IWriteService>();
    }

    [Test]
    public async Task GetAttachments_FailedGetAttachments()
    {
        // Arrange

        _client.GetAttachments(ProjectId, 1, ArtifactId)
            .Throws(new Exception("Failed to get attachments"));

        var attachmentService = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await attachmentService.GetAttachments(_testCaseId, ProjectId, ArtifactType.TestCase, ArtifactId));

        // Assert
        await _client.DidNotReceive()
            .DownloadAttachment(Arg.Any<int>(), Arg.Any<int>());

        await _writeService.DidNotReceive()
            .WriteAttachment(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<string>());
    }

    [Test]
    public async Task GetAttachments_FailedDownloadAttachment()
    {
        // Arrange
        var attachments = new List<SpiraAttachment>
        {
            new()
            {
                Id = 1,
                Name = "Test.txt",
                Type = "File"
            }
        };

        _client.GetAttachments(ProjectId, 1, ArtifactId)
            .Returns(attachments);

        _client.DownloadAttachment(ProjectId, 1)
            .Throws(new Exception("Failed to download attachment"));

        var attachmentService = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await attachmentService.GetAttachments(_testCaseId, ProjectId, ArtifactType.TestCase, ArtifactId));

        // Assert
        await _writeService.DidNotReceive()
            .WriteAttachment(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<string>());
    }

    [Test]
    public async Task GetAttachments_FailedWriteAttachment()
    {
        // Arrange
        var attachments = new List<SpiraAttachment>
        {
            new()
            {
                Id = 1,
                Name = "Test.txt",
                Type = "File"
            }
        };

        _client.GetAttachments(ProjectId, 1, ArtifactId)
            .Returns(attachments);

        _client.DownloadAttachment(ProjectId, 1)
            .Returns(Encoding.UTF8.GetBytes("Test"));

        _writeService.WriteAttachment(_testCaseId, Arg.Any<byte[]>(), Arg.Any<string>())
            .Throws(new Exception("Failed to write attachment"));

        var attachmentService = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await attachmentService.GetAttachments(_testCaseId, ProjectId, ArtifactType.TestCase, ArtifactId));
    }

    [Test]
    public async Task GetAttachments_Success()
    {
        // Arrange
        var attachments = new List<SpiraAttachment>
        {
            new()
            {
                Id = 1,
                Name = "Test.txt",
                Type = "File"
            }
        };

        _client.GetAttachments(ProjectId, 1, ArtifactId)
            .Returns(attachments);

        _client.DownloadAttachment(ProjectId, 1)
            .Returns(Encoding.UTF8.GetBytes("Test"));

        _writeService.WriteAttachment(_testCaseId, Arg.Any<byte[]>(), Arg.Any<string>())
            .Returns("Test.txt");

        var attachmentService = new AttachmentService(_logger, _client, _writeService);

        // Act
        var result = await attachmentService.GetAttachments(_testCaseId, ProjectId, ArtifactType.TestCase, ArtifactId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0], Is.EqualTo("Test.txt"));
    }
}
