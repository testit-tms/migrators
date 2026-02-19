using Moq;
using NUnit.Framework;
using Models;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models.Attachment;
using ZephyrScaleServerExporter.Models.TestCases;
using ZephyrScaleServerExporter.Services;
using ZephyrScaleServerExporter.Services.Implementations;
using ZephyrScaleServerExporter.Services.Helpers;

namespace ZephyrScaleServerExporterTests.Services;

[TestFixture]
public class AttachmentServiceTests
{
    private Mock<IDetailedLogService> _mockDetailedLogService;
    private Mock<IWriteService> _mockWriteService;
    private Mock<IClient> _mockClient;
    private AttachmentService _attachmentService;

    [SetUp]
    public void SetUp()
    {
        _mockDetailedLogService = new Mock<IDetailedLogService>();
        _mockWriteService = new Mock<IWriteService>();
        _mockClient = new Mock<IClient>();
        _attachmentService = new AttachmentService(
            _mockDetailedLogService.Object,
            _mockWriteService.Object,
            _mockClient.Object);
    }

    #region CopySharedAttachments

    [Test]
    public async Task CopySharedAttachments_WithMultipleAttachments_CallsCopyAttachmentForAll()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var step = new Step
        {
            Action = "Action",
            Expected = "Expected",
            TestData = "TestData",
            ActionAttachments = new List<string> { "file1.png", "file2.pdf" },
            ExpectedAttachments = new List<string> { "file3.docx" },
            TestDataAttachments = new List<string> { "file4.xlsx" }
        };

        _mockWriteService.Setup(x => x.CopyAttachment(targetId, "file1.png"))
            .ReturnsAsync("file1.png");
        _mockWriteService.Setup(x => x.CopyAttachment(targetId, "file2.pdf"))
            .ReturnsAsync("file2.pdf");
        _mockWriteService.Setup(x => x.CopyAttachment(targetId, "file3.docx"))
            .ReturnsAsync("file3.docx");
        _mockWriteService.Setup(x => x.CopyAttachment(targetId, "file4.xlsx"))
            .ReturnsAsync("file4.xlsx");

        // Act
        var result = await _attachmentService.CopySharedAttachments(targetId, step);

        // Assert
        Assert.Multiple(() => {
            Assert.That(result, Has.Count.EqualTo(4));
            Assert.That(result, Contains.Item("file1.png"));
            Assert.That(result, Contains.Item("file2.pdf"));
            Assert.That(result, Contains.Item("file3.docx"));
            Assert.That(result, Contains.Item("file4.xlsx"));
        });

        _mockWriteService.Verify(x => x.CopyAttachment(targetId, "file1.png"), Times.Once);
        _mockWriteService.Verify(x => x.CopyAttachment(targetId, "file2.pdf"), Times.Once);
        _mockWriteService.Verify(x => x.CopyAttachment(targetId, "file3.docx"), Times.Once);
        _mockWriteService.Verify(x => x.CopyAttachment(targetId, "file4.xlsx"), Times.Once);
    }

    [Test]
    public async Task CopySharedAttachments_WithSomeFailedCopies_ReturnsOnlySuccessfulCopies()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var step = new Step
        {
            Action = "Action",
            Expected = "Expected",
            TestData = "TestData",
            ActionAttachments = new List<string> { "file1.png", "file2.pdf" },
            ExpectedAttachments = new List<string>(),
            TestDataAttachments = new List<string>()
        };

        _mockWriteService.Setup(x => x.CopyAttachment(targetId, "file1.png"))
            .ReturnsAsync("file1.png");
        _mockWriteService.Setup(x => x.CopyAttachment(targetId, "file2.pdf"))
            .ReturnsAsync((string?)null); // Simulate failed copy

        // Act
        var result = await _attachmentService.CopySharedAttachments(targetId, step);

        // Assert
        Assert.Multiple(() => {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result, Contains.Item("file1.png"));
            Assert.That(result, Does.Not.Contain("file2.pdf"));
        });
    }

    [Test]
    public async Task CopySharedAttachments_WithNoAttachments_ReturnsEmptyList()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var step = new Step
        {
            Action = "Action",
            Expected = "Expected",
            TestData = "TestData",
            ActionAttachments = new List<string>(),
            ExpectedAttachments = new List<string>(),
            TestDataAttachments = new List<string>()
        };

        // Act
        var result = await _attachmentService.CopySharedAttachments(targetId, step);

        // Assert
        Assert.That(result, Is.Empty);
        _mockWriteService.Verify(x => x.CopyAttachment(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region DownloadAttachment

    [Test]
    public async Task DownloadAttachment_NormalExecutionFlow_DownloadsAndWritesAttachment()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var attachment = new ZephyrAttachment
        {
            FileName = "test file.png",
            Url = "https://example.com/test%20file.png"
        };
        var isSharedAttachment = false;
        var fileBytes = new byte[] { 1, 2, 3, 4 };
        var expectedFileName = "test_file.png";
        var writtenFilePath = "/path/to/test_file.png";

        _mockClient.Setup(x => x.DownloadAttachment(attachment.Url, testCaseId))
            .ReturnsAsync(fileBytes);
        _mockWriteService.Setup(x => x.WriteAttachment(testCaseId, fileBytes, expectedFileName, isSharedAttachment))
            .ReturnsAsync(writtenFilePath);

        // Act
        var result = await _attachmentService.DownloadAttachment(testCaseId, attachment, isSharedAttachment);

        // Assert
        Assert.That(result, Is.EqualTo(writtenFilePath));
        _mockDetailedLogService.Verify(x => x.LogDebug("Downloading attachment {@Attachment}", attachment), Times.Once);
        _mockClient.Verify(x => x.DownloadAttachment(attachment.Url, testCaseId), Times.Once);
        _mockWriteService.Verify(x => x.WriteAttachment(testCaseId, fileBytes, expectedFileName, isSharedAttachment), Times.Once);
    }

    [Test]
    public async Task DownloadAttachment_WithSpacesInFileName_ReplacesSpacesWithUnderscores()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var attachment = new ZephyrAttachment
        {
            FileName = "test file with spaces.png",
            Url = "https://example.com/test%20file%20with%20spaces.png"
        };
        var isSharedAttachment = true;
        var fileBytes = new byte[] { 1, 2, 3, 4 };
        var expectedFileName = "test_file_with_spaces.png";
        var writtenFilePath = "/path/to/test_file_with_spaces.png";

        _mockClient.Setup(x => x.DownloadAttachment(attachment.Url, testCaseId))
            .ReturnsAsync(fileBytes);
        _mockWriteService.Setup(x => x.WriteAttachment(testCaseId, fileBytes, expectedFileName, isSharedAttachment))
            .ReturnsAsync(writtenFilePath);

        // Act
        var result = await _attachmentService.DownloadAttachment(testCaseId, attachment, isSharedAttachment);

        // Assert
        Assert.That(result, Is.EqualTo(writtenFilePath));
        _mockWriteService.Verify(x => x.WriteAttachment(testCaseId, fileBytes, expectedFileName, isSharedAttachment), Times.Once);
    }

    [Test]
    public async Task DownloadAttachment_WithInvalidCharsInFileName_ReplacesInvalidChars()
    {
        // Arrange - use same sanitization as AttachmentService (platform-independent)
        var testCaseId = Guid.NewGuid();
        var rawFileName = "test<file>:name?.png";
        var attachment = new ZephyrAttachment
        {
            FileName = rawFileName,
            Url = "https://example.com/test%3Cfile%3E%3Aname%3F.png"
        };
        var isSharedAttachment = false;
        var fileBytes = new byte[] { 1, 2, 3, 4 };
        var expectedFileName = Utils.ReplaceInvalidChars(Utils.SpacesToUnderscores(rawFileName));
        var writtenFilePath = "/path/to/" + expectedFileName;

        _mockClient.Setup(x => x.DownloadAttachment(attachment.Url, testCaseId))
            .ReturnsAsync(fileBytes);
        _mockWriteService.Setup(x => x.WriteAttachment(testCaseId, fileBytes, expectedFileName, isSharedAttachment))
            .ReturnsAsync(writtenFilePath);

        // Act
        var result = await _attachmentService.DownloadAttachment(testCaseId, attachment, isSharedAttachment);

        // Assert
        Assert.That(result, Is.EqualTo(writtenFilePath));
        _mockWriteService.Verify(x => x.WriteAttachment(testCaseId, fileBytes, expectedFileName, isSharedAttachment), Times.Once);
    }

    #endregion

    #region DownloadAttachmentById

    [Test]
    public async Task DownloadAttachmentById_NormalExecutionFlow_DownloadsAndWritesAttachment()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var attachment = new StepAttachment
        {
            Id = 123,
            Name = "test file.png"
        };
        var isSharedAttachment = false;
        var fileBytes = new byte[] { 1, 2, 3, 4 };
        var expectedFileName = "test_file.png";
        var writtenFilePath = "/path/to/test_file.png";

        _mockClient.Setup(x => x.DownloadAttachmentById(attachment.Id, testCaseId))
            .ReturnsAsync(fileBytes);
        _mockWriteService.Setup(x => x.WriteAttachment(testCaseId, fileBytes, expectedFileName, isSharedAttachment))
            .ReturnsAsync(writtenFilePath);

        // Act
        var result = await _attachmentService.DownloadAttachmentById(testCaseId, attachment, isSharedAttachment);

        // Assert
        Assert.That(result, Is.EqualTo(writtenFilePath));
        _mockDetailedLogService.Verify(x => x.LogDebug("Downloading attachment by id {@Attachment}", attachment), Times.Once);
        _mockClient.Verify(x => x.DownloadAttachmentById(attachment.Id, testCaseId), Times.Once);
        _mockWriteService.Verify(x => x.WriteAttachment(testCaseId, fileBytes, expectedFileName, isSharedAttachment), Times.Once);
    }

    [Test]
    public async Task DownloadAttachmentById_WithSpacesInName_ReplacesSpacesWithUnderscores()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var attachment = new StepAttachment
        {
            Id = 456,
            Name = "test file with spaces.png"
        };
        var isSharedAttachment = true;
        var fileBytes = new byte[] { 1, 2, 3, 4 };
        var expectedFileName = "test_file_with_spaces.png";
        var writtenFilePath = "/path/to/test_file_with_spaces.png";

        _mockClient.Setup(x => x.DownloadAttachmentById(attachment.Id, testCaseId))
            .ReturnsAsync(fileBytes);
        _mockWriteService.Setup(x => x.WriteAttachment(testCaseId, fileBytes, expectedFileName, isSharedAttachment))
            .ReturnsAsync(writtenFilePath);

        // Act
        var result = await _attachmentService.DownloadAttachmentById(testCaseId, attachment, isSharedAttachment);

        // Assert
        Assert.That(result, Is.EqualTo(writtenFilePath));
        _mockWriteService.Verify(x => x.WriteAttachment(testCaseId, fileBytes, expectedFileName, isSharedAttachment), Times.Once);
    }

    [Test]
    public async Task DownloadAttachmentById_WithInvalidCharsInName_ReplacesInvalidChars()
    {
        // Arrange - use same sanitization as AttachmentService (platform-independent)
        var testCaseId = Guid.NewGuid();
        var rawName = "test<file>:name?.png";
        var attachment = new StepAttachment
        {
            Id = 789,
            Name = rawName
        };
        var isSharedAttachment = false;
        var fileBytes = new byte[] { 1, 2, 3, 4 };
        var expectedFileName = Utils.ReplaceInvalidChars(Utils.SpacesToUnderscores(rawName));
        var writtenFilePath = "/path/to/" + expectedFileName;

        _mockClient.Setup(x => x.DownloadAttachmentById(attachment.Id, testCaseId))
            .ReturnsAsync(fileBytes);
        _mockWriteService.Setup(x => x.WriteAttachment(testCaseId, fileBytes, expectedFileName, isSharedAttachment))
            .ReturnsAsync(writtenFilePath);

        // Act
        var result = await _attachmentService.DownloadAttachmentById(testCaseId, attachment, isSharedAttachment);

        // Assert
        Assert.That(result, Is.EqualTo(writtenFilePath));
        _mockWriteService.Verify(x => x.WriteAttachment(testCaseId, fileBytes, expectedFileName, isSharedAttachment), Times.Once);
    }

    #endregion
}
