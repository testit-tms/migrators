using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models;
using ZephyrScaleServerExporter.Models.Attachment;
using ZephyrScaleServerExporter.Models.TestCases;
using ZephyrScaleServerExporter.Services;
using ZephyrScaleServerExporter.Services.TestCase.Implementations;
using ZephyrScaleServerExporterTests.Helpers;

namespace ZephyrScaleServerExporterTests.Services.TestCase;

[TestFixture]
public class TestCaseAttachmentsServiceTests
{
    private Mock<IClient> _mockClient;
    private Mock<IOptions<AppConfig>> _mockAppConfig;
    private Mock<IAttachmentService> _mockAttachmentService;
    private TestCaseAttachmentsService _testCaseAttachmentsService;

    private Guid _testCaseId;
    private AppConfig _appConfig;

    [SetUp]
    public void SetUp()
    {
        _mockClient = new Mock<IClient>();
        _mockAppConfig = new Mock<IOptions<AppConfig>>();
        _mockAttachmentService = new Mock<IAttachmentService>();

        _appConfig = new AppConfig
        {
            ResultPath = "test_path",
            Zephyr = new ZephyrConfig
            {
                Url = "https://zephyr.example.com"
            }
        };
        _mockAppConfig.Setup(c => c.Value).Returns(_appConfig);

        _testCaseAttachmentsService = new TestCaseAttachmentsService(
            _mockClient.Object,
            _mockAppConfig.Object,
            _mockAttachmentService.Object);

        _testCaseId = Guid.NewGuid();
    }

    #region FillAttachments

    [Test]
    public async Task FillAttachments_WithActiveTestCase_ReturnsAllAttachments()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1");
        var description = TestDataHelper.CreateZephyrDescriptionData(
            attachments: new List<ZephyrAttachment>
            {
                TestDataHelper.CreateZephyrAttachment("desc_attachment.txt", "/rest/attachment/2")
            });

        var apiAttachments = new List<ZephyrAttachment>
        {
            TestDataHelper.CreateZephyrAttachment("api_attachment.txt", "/rest/attachment/1")
        };

        _mockClient
            .Setup(c => c.GetAttachmentsForTestCase("TEST-1"))
            .ReturnsAsync(apiAttachments);

        _mockAttachmentService
            .Setup(s => s.DownloadAttachment(_testCaseId, It.IsAny<ZephyrAttachment>(), false))
            .ReturnsAsync((Guid id, ZephyrAttachment att, bool shared) => att.FileName);

        // Act
        var result = await _testCaseAttachmentsService.FillAttachments(
            _testCaseId,
            zephyrTestCase,
            description);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result, Contains.Item("api_attachment.txt"));
            Assert.That(result, Contains.Item("desc_attachment.txt"));
        });

        _mockClient.Verify(c => c.GetAttachmentsForTestCase("TEST-1"), Times.Once);

        _mockAttachmentService.Verify(s => s.DownloadAttachment(
            _testCaseId,
            It.IsAny<ZephyrAttachment>(),
            false), Times.Exactly(2));
    }

    [Test]
    public async Task FillAttachments_WithArchivedTestCaseAndJiraId_UsesGetAltAttachmentsForTestCase()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1", jiraId: "10001", isArchived: true);
        var description = TestDataHelper.CreateZephyrDescriptionData();
        var altAttachments = new List<AltAttachmentResult>
        {
            new AltAttachmentResult
            {
                FileName = "archived_attachment.txt",
                Id = 123,
                ProjectId = 1
            }
        };

        _mockClient
            .Setup(c => c.GetAltAttachmentsForTestCase("10001"))
            .ReturnsAsync(altAttachments);

        _mockAttachmentService
            .Setup(s => s.DownloadAttachment(_testCaseId, It.IsAny<ZephyrAttachment>(), false))
            .ReturnsAsync((Guid id, ZephyrAttachment att, bool shared) => att.FileName);

        // Act
        var result = await _testCaseAttachmentsService.FillAttachments(
            _testCaseId,
            zephyrTestCase,
            description);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(1));
        });

        _mockClient.Verify(c => c.GetAltAttachmentsForTestCase("10001"), Times.Once);
        _mockClient.Verify(c => c.GetAttachmentsForTestCase(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task FillAttachments_WithArchivedTestCaseWithoutJiraId_UsesGetAttachmentsForTestCase()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1", isArchived: true);
        var description = TestDataHelper.CreateZephyrDescriptionData();
        var apiAttachments = new List<ZephyrAttachment>
        {
            TestDataHelper.CreateZephyrAttachment("attachment.txt", "/rest/attachment/1")
        };

        _mockClient
            .Setup(c => c.GetAttachmentsForTestCase("TEST-1"))
            .ReturnsAsync(apiAttachments);

        _mockAttachmentService
            .Setup(s => s.DownloadAttachment(_testCaseId, It.IsAny<ZephyrAttachment>(), false))
            .ReturnsAsync((Guid id, ZephyrAttachment att, bool shared) => att.FileName);

        // Act
        var result = await _testCaseAttachmentsService.FillAttachments(
            _testCaseId,
            zephyrTestCase,
            description);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        _mockClient.Verify(c => c.GetAttachmentsForTestCase("TEST-1"), Times.Once);
        _mockClient.Verify(c => c.GetAltAttachmentsForTestCase(It.IsAny<string>()), Times.Never);
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    public async Task FillAttachments_WithNullOrEmptyDescription_HandlesGracefully(string? descriptionText)
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1");
        var description = TestDataHelper.CreateZephyrDescriptionData(description: descriptionText);
        var apiAttachments = new List<ZephyrAttachment>
        {
            TestDataHelper.CreateZephyrAttachment("attachment.txt", "/rest/attachment/1")
        };

        _mockClient
            .Setup(c => c.GetAttachmentsForTestCase("TEST-1"))
            .ReturnsAsync(apiAttachments);

        _mockAttachmentService
            .Setup(s => s.DownloadAttachment(_testCaseId, It.IsAny<ZephyrAttachment>(), false))
            .ReturnsAsync((Guid id, ZephyrAttachment att, bool shared) => att.FileName);

        // Act
        var result = await _testCaseAttachmentsService.FillAttachments(
            _testCaseId,
            zephyrTestCase,
            description);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task FillAttachments_WithException_ReturnsEmptyList()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1");
        var description = TestDataHelper.CreateZephyrDescriptionData();
        var exception = new Exception("Client error");

        _mockClient
            .Setup(c => c.GetAttachmentsForTestCase("TEST-1"))
            .ThrowsAsync(exception);

        // Act
        var result = await _testCaseAttachmentsService.FillAttachments(
            _testCaseId,
            zephyrTestCase,
            description);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task FillAttachments_WithEmptyAttachments_ReturnsEmptyList()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1");
        var description = TestDataHelper.CreateZephyrDescriptionData();

        _mockClient
            .Setup(c => c.GetAttachmentsForTestCase("TEST-1"))
            .ReturnsAsync(new List<ZephyrAttachment>());

        // Act
        var result = await _testCaseAttachmentsService.FillAttachments(
            _testCaseId,
            zephyrTestCase,
            description);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task FillAttachments_WithDuplicateAttachments_Deduplicates()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1");
        var duplicateAttachment = TestDataHelper.CreateZephyrAttachment("duplicate.txt", "/rest/attachment/1");
        var description = TestDataHelper.CreateZephyrDescriptionData(
            attachments: new List<ZephyrAttachment> { duplicateAttachment });

        var apiAttachments = new List<ZephyrAttachment> { duplicateAttachment };

        _mockClient
            .Setup(c => c.GetAttachmentsForTestCase("TEST-1"))
            .ReturnsAsync(apiAttachments);

        _mockAttachmentService
            .Setup(s => s.DownloadAttachment(_testCaseId, duplicateAttachment, false))
            .ReturnsAsync("duplicate.txt");

        // Act
        var result = await _testCaseAttachmentsService.FillAttachments(
            _testCaseId,
            zephyrTestCase,
            description);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result, Contains.Item("duplicate.txt"));
        });

        _mockAttachmentService.Verify(s => s.DownloadAttachment(
            _testCaseId,
            duplicateAttachment,
            false), Times.Once);
    }

    #endregion

    #region CalcPreconditionAttachments

    [Test]
    public async Task CalcPreconditionAttachments_WithAttachments_ReturnsPreconditionAttachments()
    {
        // Arrange
        var precondition = TestDataHelper.CreateZephyrDescriptionData(
            attachments: new List<ZephyrAttachment>
            {
                TestDataHelper.CreateZephyrAttachment("precondition1.txt", "/rest/attachment/1"),
                TestDataHelper.CreateZephyrAttachment("precondition2.txt", "/rest/attachment/2")
            });
        var attachments = new List<string>();

        _mockAttachmentService
            .Setup(s => s.DownloadAttachment(_testCaseId, It.IsAny<ZephyrAttachment>(), false))
            .ReturnsAsync((Guid id, ZephyrAttachment att, bool shared) => att.FileName);

        // Act
        var result = await _testCaseAttachmentsService.CalcPreconditionAttachments(
            _testCaseId,
            precondition,
            attachments);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result, Contains.Item("precondition1.txt"));
            Assert.That(result, Contains.Item("precondition2.txt"));
            Assert.That(attachments, Has.Count.EqualTo(2));
            Assert.That(attachments, Contains.Item("precondition1.txt"));
            Assert.That(attachments, Contains.Item("precondition2.txt"));
        });

        _mockAttachmentService.Verify(s => s.DownloadAttachment(
            _testCaseId,
            It.IsAny<ZephyrAttachment>(),
            false), Times.Exactly(2));
    }

    [Test]
    public async Task CalcPreconditionAttachments_WithEmptyPreconditionAttachments_ReturnsEmptyList()
    {
        // Arrange
        var precondition = TestDataHelper.CreateZephyrDescriptionData(
            attachments: new List<ZephyrAttachment>());
        var attachments = new List<string>();

        // Act
        var result = await _testCaseAttachmentsService.CalcPreconditionAttachments(
            _testCaseId,
            precondition,
            attachments);

        // Assert
        Assert.That(result, Is.Empty);
        _mockAttachmentService.Verify(s => s.DownloadAttachment(
            It.IsAny<Guid>(),
            It.IsAny<ZephyrAttachment>(),
            It.IsAny<bool>()), Times.Never);
    }

    [Test]
    public async Task CalcPreconditionAttachments_WithDuplicateAttachments_Deduplicates()
    {
        // Arrange
        var duplicateAttachment = TestDataHelper.CreateZephyrAttachment("duplicate.txt", "/rest/attachment/1");
        var precondition = TestDataHelper.CreateZephyrDescriptionData(
            attachments: new List<ZephyrAttachment> { duplicateAttachment });
        var attachments = new List<string> { "duplicate.txt" };

        _mockAttachmentService
            .Setup(s => s.DownloadAttachment(_testCaseId, duplicateAttachment, false))
            .ReturnsAsync("duplicate.txt");

        // Act
        var result = await _testCaseAttachmentsService.CalcPreconditionAttachments(
            _testCaseId,
            precondition,
            attachments);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(attachments, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task CalcPreconditionAttachments_WithAttachmentServiceException_PropagatesException()
    {
        // Arrange
        var precondition = TestDataHelper.CreateZephyrDescriptionData(
            attachments: new List<ZephyrAttachment>
            {
                TestDataHelper.CreateZephyrAttachment("attachment.txt", "/rest/attachment/1")
            });
        var attachments = new List<string>();
        var exception = new Exception("Download error");

        _mockAttachmentService
            .Setup(s => s.DownloadAttachment(_testCaseId, It.IsAny<ZephyrAttachment>(), false))
            .ThrowsAsync(exception);

        // Act & Assert
        Assert.That(
            async () => await _testCaseAttachmentsService.CalcPreconditionAttachments(
                _testCaseId,
                precondition,
                attachments),
            Throws.Exception.EqualTo(exception));
    }

    #endregion
}
