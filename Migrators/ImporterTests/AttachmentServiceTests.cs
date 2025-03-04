using Importer.Client;
using Importer.Services;
using Importer.Services.Implementations;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ImporterTests;

public class AttachmentServiceTests
{
    private ILogger<AttachmentService> _logger = null!;
    private IClientAdapter _clientAdapter = null!;
    private IParserService _parserService = null!;
    private AttachmentService _attachmentService = null!;

    private static readonly Guid WorkItemId = Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbee7");
    private static readonly Guid AttachmentId1 = Guid.Parse("9767ce0e-a214-4ebc-af69-71aa88b0ad0d");
    private static readonly Guid AttachmentId2 = Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbe10");

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<AttachmentService>>();
        _clientAdapter = Substitute.For<IClientAdapter>();
        _parserService = Substitute.For<IParserService>();

        _attachmentService = new AttachmentService(_logger, _clientAdapter, _parserService);
    }

    [Test]
    public async Task GetAttachments_WhenNoAttachments_ReturnsEmptyDictionary()
    {
        // Arrange
        var attachments = Array.Empty<string>();

        // Act
        var result = await _attachmentService.GetAttachments(WorkItemId, attachments);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
            await _parserService.DidNotReceive().GetAttachment(Arg.Any<Guid>(), Arg.Any<string>());
            await _clientAdapter.DidNotReceive().UploadAttachment(Arg.Any<string>(), Arg.Any<Stream>());
        });
    }

    [Test]
    public async Task GetAttachments_WhenGetAttachmentFails_ThrowsException()
    {
        // Arrange
        var attachments = new[] { "test.txt" };
        _parserService.GetAttachment(WorkItemId, attachments[0])
            .ThrowsAsync(new Exception("Failed to get attachment"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(
            async () => await _attachmentService.GetAttachments(WorkItemId, attachments));

        Assert.Multiple(async () =>
        {
            Assert.That(ex!.Message, Is.EqualTo("Failed to get attachment"));
            await _clientAdapter.DidNotReceive().UploadAttachment(Arg.Any<string>(), Arg.Any<Stream>());
        });
    }

    [Test]
    public async Task GetAttachments_WhenUploadAttachmentFails_ThrowsException()
    {
        // Arrange
        var attachments = new[] { "test.txt" };
        var stream = new FileStream("test.txt", FileMode.Create);
        _parserService.GetAttachment(WorkItemId, attachments[0]).Returns(stream);
        _clientAdapter.UploadAttachment(Arg.Any<string>(), Arg.Any<Stream>())
            .ThrowsAsync(new Exception("Failed to upload attachment"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(
            async () => await _attachmentService.GetAttachments(WorkItemId, attachments));

        Assert.Multiple(async () =>
        {
            Assert.That(ex!.Message, Is.EqualTo("Failed to upload attachment"));
            await _parserService.Received(1).GetAttachment(WorkItemId, attachments[0]);
        });
    }

    [Test]
    public async Task GetAttachments_WhenSuccessful_ReturnsDictionary()
    {
        // Arrange
        var attachments = new[] { "test1.txt", "test2.txt" };
        var stream1 = new FileStream("test1.txt", FileMode.Create);
        var stream2 = new FileStream("test2.txt", FileMode.Create);

        _parserService.GetAttachment(WorkItemId, attachments[0]).Returns(stream1);
        _parserService.GetAttachment(WorkItemId, attachments[1]).Returns(stream2);
        _clientAdapter.UploadAttachment(attachments[0], stream1).Returns(AttachmentId1);
        _clientAdapter.UploadAttachment(attachments[1], stream2).Returns(AttachmentId2);

        // Act
        var result = await _attachmentService.GetAttachments(WorkItemId, attachments);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[attachments[0]], Is.EqualTo(AttachmentId1));
            Assert.That(result[attachments[1]], Is.EqualTo(AttachmentId2));
            
            await _parserService.Received(1).GetAttachment(WorkItemId, attachments[0]);
            await _parserService.Received(1).GetAttachment(WorkItemId, attachments[1]);
            await _clientAdapter.Received(1).UploadAttachment(attachments[0], stream1);
            await _clientAdapter.Received(1).UploadAttachment(attachments[1], stream2);
        });
    }
} 