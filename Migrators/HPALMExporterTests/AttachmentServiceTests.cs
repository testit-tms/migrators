using HPALMExporter.Client;
using HPALMExporter.Models;
using HPALMExporter.Services;
using JsonWriter;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace HPALMExporterTests;

public class AttachmentServiceTests
{
    private ILogger<AttachmentService> _logger;
    private IClient _client;
    private IWriteService _writeService;

    private const int TestId = 1;
    private readonly Guid _testCaseId = Guid.NewGuid();
    private List<HPALMAttachment> _attachments;
    private byte[] _attachmentData = { 1, 2, 3 };

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<AttachmentService>>();
        _client = Substitute.For<IClient>();
        _writeService = Substitute.For<IWriteService>();

        _attachments = new List<HPALMAttachment>
        {
            new()
            {
                Id = 1,
                Name = "Attachment 1",
                Description = "https://www.google.com",
                Type = HPALMAttachmentType.Url
            },
            new()
            {
                Id = 2,
                Name = "Attachment 2",
                Description = "File",
                Type = HPALMAttachmentType.File
            }
        };
    }


    [Test]
    public async Task ConvertAttachmentsFromTest_FailedGetAttachmentsFromTest()
    {
        // Arrange
        _client.GetAttachmentsFromTest(TestId)
            .Throws(new Exception("Failed to get attachments from test"));

        var attachmentService = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(() => attachmentService.ConvertAttachmentsFromTest(_testCaseId, TestId));

        // Assert
        await _client.DidNotReceive()
            .DownloadAttachment(Arg.Any<int>(), Arg.Any<string>());

        await _writeService.DidNotReceive()
            .WriteAttachment(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<string>());
    }

    [Test]
    public async Task ConvertAttachmentsFromTest_FailedDownloadAttachment()
    {
        // Arrange
        _client.GetAttachmentsFromTest(TestId)
            .Returns(_attachments);

        _client.DownloadAttachment(TestId, _attachments[1].Name)
            .Throws(new Exception("Failed to download attachment"));

        var attachmentService = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(() => attachmentService.ConvertAttachmentsFromTest(_testCaseId, TestId));

        // Assert
        await _writeService.DidNotReceive()
            .WriteAttachment(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<string>());
    }

    [Test]
    public async Task ConvertAttachmentsFromTest_FailedWriteAttachment()
    {
        // Arrange
        _client.GetAttachmentsFromTest(TestId)
            .Returns(_attachments);

        _client.DownloadAttachment(TestId, _attachments[1].Name)
            .Returns(_attachmentData);

        _writeService.WriteAttachment(_testCaseId, _attachmentData, _attachments[1].Name)
            .Throws(new Exception("Failed to write attachment"));

        var attachmentService = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(() => attachmentService.ConvertAttachmentsFromTest(_testCaseId, TestId));
    }

    [Test]
    public async Task ConvertAttachmentsFromTest_Success()
    {
        // Arrange
        _client.GetAttachmentsFromTest(TestId)
            .Returns(_attachments);

        _client.DownloadAttachment(TestId, _attachments[1].Name)
            .Returns(_attachmentData);

        _writeService.WriteAttachment(_testCaseId, _attachmentData, _attachments[1].Name)
            .Returns(_attachments[1].Name);

        var attachmentService = new AttachmentService(_logger, _client, _writeService);

        // Act
        var attachmentData = await attachmentService.ConvertAttachmentsFromTest(_testCaseId, TestId);

        // Assert
        Assert.That(attachmentData.Attachments, Has.Count.EqualTo(1));
        Assert.That(attachmentData.Attachments[0], Is.EqualTo(_attachments[1].Name));
        Assert.That(attachmentData.Links, Has.Count.EqualTo(1));
        Assert.That(attachmentData.Links[0].Title, Is.EqualTo(_attachments[0].Name));
        Assert.That(attachmentData.Links[0].Url, Is.EqualTo(_attachments[0].Description));
    }

    [Test]
    public async Task ConvertAttachmentsFromStep_FailedGetAttachmentsFromTest()
    {
        // Arrange
        _client.GetAttachmentsFromStep(TestId)
            .Throws(new Exception("Failed to get attachments from step"));

        var attachmentService = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(() => attachmentService.ConvertAttachmentsFromStep(_testCaseId, TestId));

        // Assert
        await _client.DidNotReceive()
            .DownloadAttachment(Arg.Any<int>(), Arg.Any<string>());

        await _writeService.DidNotReceive()
            .WriteAttachment(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<string>());
    }

    [Test]
    public async Task ConvertAttachmentsFromStep_FailedDownloadAttachment()
    {
        // Arrange
        _client.GetAttachmentsFromStep(TestId)
            .Returns(_attachments);

        _client.DownloadAttachment(TestId, _attachments[1].Name)
            .Throws(new Exception("Failed to download attachment"));

        var attachmentService = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(() => attachmentService.ConvertAttachmentsFromStep(_testCaseId, TestId));

        // Assert
        await _writeService.DidNotReceive()
            .WriteAttachment(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<string>());
    }

    [Test]
    public async Task ConvertAttachmentsFromStep_FailedWriteAttachment()
    {
        // Arrange
        _client.GetAttachmentsFromStep(TestId)
            .Returns(_attachments);

        _client.DownloadAttachment(TestId, _attachments[1].Name)
            .Returns(_attachmentData);

        _writeService.WriteAttachment(_testCaseId, _attachmentData, _attachments[1].Name)
            .Throws(new Exception("Failed to write attachment"));

        var attachmentService = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(() => attachmentService.ConvertAttachmentsFromStep(_testCaseId, TestId));
    }

    [Test]
    public async Task ConvertAttachmentsFromStep_Success()
    {
        // Arrange
        _client.GetAttachmentsFromStep(TestId)
            .Returns(_attachments);

        _client.DownloadAttachment(TestId, _attachments[1].Name)
            .Returns(_attachmentData);

        _writeService.WriteAttachment(_testCaseId, _attachmentData, _attachments[1].Name)
            .Returns(_attachments[1].Name);

        var attachmentService = new AttachmentService(_logger, _client, _writeService);

        // Act
        var attachmentData = await attachmentService.ConvertAttachmentsFromStep(_testCaseId, TestId);

        // Assert
        Assert.That(attachmentData.Attachments, Has.Count.EqualTo(1));
        Assert.That(attachmentData.Attachments[0], Is.EqualTo(_attachments[1].Name));
        Assert.That(attachmentData.Links, Has.Count.EqualTo(1));
        Assert.That(attachmentData.Links[0].Title, Is.EqualTo(_attachments[0].Name));
        Assert.That(attachmentData.Links[0].Url, Is.EqualTo(_attachments[0].Description));
    }
}
