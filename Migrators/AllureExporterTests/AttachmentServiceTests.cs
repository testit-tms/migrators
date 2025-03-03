using AllureExporter.Client;
using AllureExporter.Models.Attachment;
using AllureExporter.Services.Implementations;
using JsonWriter;
using Microsoft.Extensions.Logging;
using Moq;

namespace AllureExporterTests;

public class AttachmentServiceTests
{
    private Mock<ILogger<AttachmentService>> _logger;
    private Mock<IClient> _client;
    private Mock<IWriteService> _writeService;
    private AttachmentService _sut;
    private List<AllureAttachment> _attachments;
    private readonly Guid _testId = Guid.NewGuid();
    private const int TestCaseId = 1;
    private const int SharedStepId = 2;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<AttachmentService>>();
        _client = new Mock<IClient>();
        _writeService = new Mock<IWriteService>();
        _sut = new AttachmentService(_logger.Object, _client.Object, _writeService.Object);
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
    public async Task DownloadAttachmentsforTestCase_Success()
    {
        // Arrange
        var bytes = new byte[] { 1, 2, 3 };
        _client.Setup(x => x.GetAttachmentsByTestCaseId(TestCaseId))
            .ReturnsAsync(_attachments);
        _client.Setup(x => x.DownloadAttachmentForTestCase(_attachments[0].Id))
            .ReturnsAsync(bytes);
        _client.Setup(x => x.DownloadAttachmentForTestCase(_attachments[1].Id))
            .ReturnsAsync(bytes);
        _writeService.Setup(x => x.WriteAttachment(_testId, bytes, _attachments[0].Name))
            .ReturnsAsync(_attachments[0].Name);
        _writeService.Setup(x => x.WriteAttachment(_testId, bytes, _attachments[1].Name))
            .ReturnsAsync(_attachments[1].Name);

        // Act
        var result = await _sut.DownloadAttachmentsforTestCase(TestCaseId, _testId);

        // Assert
        Assert.That(result, Is.EqualTo(new List<string> { _attachments[0].Name, _attachments[1].Name }));
        _writeService.Verify(x => x.WriteAttachment(_testId, bytes, _attachments[0].Name), Times.Once);
        _writeService.Verify(x => x.WriteAttachment(_testId, bytes, _attachments[1].Name), Times.Once);

        // Verify logging
        _logger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Downloading attachments by test case id {TestCaseId}")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    [Test]
    public async Task DownloadAttachmentsforSharedStep_Success()
    {
        // Arrange
        var bytes = new byte[] { 1, 2, 3 };
        _client.Setup(x => x.GetAttachmentsBySharedStepId(SharedStepId))
            .ReturnsAsync(_attachments);
        _client.Setup(x => x.DownloadAttachmentForSharedStep(_attachments[0].Id))
            .ReturnsAsync(bytes);
        _client.Setup(x => x.DownloadAttachmentForSharedStep(_attachments[1].Id))
            .ReturnsAsync(bytes);
        _writeService.Setup(x => x.WriteAttachment(_testId, bytes, _attachments[0].Name))
            .ReturnsAsync(_attachments[0].Name);
        _writeService.Setup(x => x.WriteAttachment(_testId, bytes, _attachments[1].Name))
            .ReturnsAsync(_attachments[1].Name);

        // Act
        var result = await _sut.DownloadAttachmentsforSharedStep(SharedStepId, _testId);

        // Assert
        Assert.That(result, Is.EqualTo(new List<string> { _attachments[0].Name, _attachments[1].Name }));
        _writeService.Verify(x => x.WriteAttachment(_testId, bytes, _attachments[0].Name), Times.Once);
        _writeService.Verify(x => x.WriteAttachment(_testId, bytes, _attachments[1].Name), Times.Once);

        // Verify logging
        _logger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Downloading attachments by shared step id {SharedStepId}")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    [Test]
    public async Task DownloadAttachmentsforTestCase_WhenNameContainsInvalidCharacters_ReplacesThemWithUnderscore()
    {
        // Arrange
        var bytes = new byte[] { 1, 2, 3 };
        var attachmentsWithInvalidNames = new List<AllureAttachment>
        {
            new() { Id = 1, Name = "test:file*.txt" },
            new() { Id = 2, Name = "test/file<>.png" }
        };
        var expectedNames = new List<string> { "test_file_.txt", "test_file__.png" };

        _client.Setup(x => x.GetAttachmentsByTestCaseId(TestCaseId))
            .ReturnsAsync(attachmentsWithInvalidNames);
        _client.Setup(x => x.DownloadAttachmentForTestCase(It.IsAny<long>()))
            .ReturnsAsync(bytes);
        _writeService.Setup(x => x.WriteAttachment(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<string>()))
            .Returns<Guid, byte[], string>((_, _, name) => Task.FromResult(name));

        // Act
        var result = await _sut.DownloadAttachmentsforTestCase(TestCaseId, _testId);

        // Assert
        Assert.That(result, Is.EqualTo(expectedNames));
        _writeService.Verify(x => x.WriteAttachment(
            It.IsAny<Guid>(),
            It.IsAny<byte[]>(),
            It.Is<string>(name => expectedNames.Contains(name))), Times.Exactly(2));
    }

    [Test]
    public void DownloadAttachmentsforTestCase_FailedGetAttachments()
    {
        // Arrange
        _client.Setup(x => x.GetAttachmentsByTestCaseId(TestCaseId))
            .ThrowsAsync(new Exception("Failed to get attachments"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(() => _sut.DownloadAttachmentsforTestCase(TestCaseId, _testId));
        Assert.That(ex.Message, Is.EqualTo("Failed to get attachments"));

        _client.Verify(x => x.DownloadAttachmentForTestCase(It.IsAny<long>()), Times.Never);
        _writeService.Verify(x => x.WriteAttachment(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void DownloadAttachmentsforTestCase_FailedDownloadAttachment()
    {
        // Arrange
        _client.Setup(x => x.GetAttachmentsByTestCaseId(TestCaseId))
            .ReturnsAsync(_attachments);
        _client.Setup(x => x.DownloadAttachmentForTestCase(_attachments[0].Id))
            .ThrowsAsync(new Exception("Failed to download attachment"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(() => _sut.DownloadAttachmentsforTestCase(TestCaseId, _testId));
        Assert.That(ex.Message, Is.EqualTo("Failed to download attachment"));

        _writeService.Verify(x => x.WriteAttachment(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void DownloadAttachmentsforTestCase_FailedWriteAttachment()
    {
        // Arrange
        var bytes = new byte[] { 1, 2, 3 };
        _client.Setup(x => x.GetAttachmentsByTestCaseId(TestCaseId))
            .ReturnsAsync(_attachments);
        _client.Setup(x => x.DownloadAttachmentForTestCase(_attachments[0].Id))
            .ReturnsAsync(bytes);
        _writeService.Setup(x => x.WriteAttachment(_testId, bytes, _attachments[0].Name))
            .ThrowsAsync(new Exception("Failed to write attachment"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(() => _sut.DownloadAttachmentsforTestCase(TestCaseId, _testId));
        Assert.That(ex.Message, Is.EqualTo("Failed to write attachment"));
    }
}
