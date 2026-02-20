using Microsoft.Extensions.Logging;
using Models;
using Moq;
using NUnit.Framework;
using System.Net;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models.Client;
using ZephyrScaleServerExporter.Models.TestCases;
using ZephyrScaleServerExporter.Services;
using ZephyrScaleServerExporter.Services.TestCase.Implementations;
using ZephyrScaleServerExporterTests.Helpers;

namespace ZephyrScaleServerExporterTests.Services.TestCase;

[TestFixture]
public class TestCaseAdditionalLinksServiceTests
{
    private Mock<IDetailedLogService> _mockDetailedLogService;
    private Mock<ILogger<TestCaseConvertService>> _mockLogger;
    private Mock<IClient> _mockClient;
    private TestCaseAdditionalLinksService _testCaseAdditionalLinksService;

    [SetUp]
    public void SetUp()
    {
        _mockDetailedLogService = new Mock<IDetailedLogService>();
        _mockLogger = new Mock<ILogger<TestCaseConvertService>>();
        _mockClient = new Mock<IClient>();

        _testCaseAdditionalLinksService = new TestCaseAdditionalLinksService(
            _mockDetailedLogService.Object,
            _mockLogger.Object,
            _mockClient.Object);

        _mockClient.Setup(c => c.GetBaseUrl()).Returns(new Uri("https://jira.example.com"));
    }

    #region GetAdditionalLinks

    [Test]
    public async Task GetAdditionalLinks_WithAllLinkTypes_ReturnsAllLinks()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1", jiraId: "10001");
        var traceLinks = new List<TraceLink>
        {
            TestDataHelper.CreateTraceLink(urlDescription: "Web Link", url: "https://example.com"),
            TestDataHelper.CreateTraceLink(confluencePageId: "12345"),
            TestDataHelper.CreateTraceLink(issueId: "10002")
        };

        var traceLinksRoot = TestDataHelper.CreateTraceLinksRoot(traceLinks, id: 10001);
        var tracesResponse = TestDataHelper.CreateTestCaseTracesResponseWrapper(
            new List<TraceLinksRoot> { traceLinksRoot });

        var confluenceLinks = new List<ZephyrConfluenceLink>
        {
            TestDataHelper.CreateZephyrConfluenceLink("Confluence Page", "https://confluence.example.com/page")
        };
        var jiraIssue = TestDataHelper.CreateJiraIssue("TEST-2", "10002", "Test Issue");

        _mockClient
            .Setup(c => c.GetTestCaseTracesV2("TEST-1", false))
            .ReturnsAsync(tracesResponse);

        _mockClient
            .Setup(c => c.GetConfluenceLinksByConfluencePageId("12345"))
            .ReturnsAsync(confluenceLinks);

        _mockClient
            .Setup(c => c.GetIssueById("10002"))
            .ReturnsAsync(jiraIssue);

        // Act
        var result = await _testCaseAdditionalLinksService.GetAdditionalLinks(zephyrTestCase);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(3));
            Assert.That(result.Any(l => l.Title == "Web Link" && l.Url == "https://example.com"), Is.True);
            Assert.That(result.Any(l => l.Title == "Confluence Page" && l.Url == "https://confluence.example.com/page"), Is.True);
            Assert.That(result.Any(l => l.Title == "Test Issue" && l.Url == "https://jira.example.com/browse/TEST-2"), Is.True);
            Assert.That(zephyrTestCase.JiraId, Is.EqualTo("10001"));
        });

        _mockDetailedLogService.Verify(s => s.LogDebug(
            It.Is<string>(msg => msg.Contains("Getting additional links")),
            It.IsAny<object[]>()), Times.Once);
    }

    [Test]
    public async Task GetAdditionalLinks_WithNullTraceLinks_ReturnsEmptyList()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1");
        var traceLinksRoot = TestDataHelper.CreateTraceLinksRoot(null, id: 1);
        var tracesResponse = TestDataHelper.CreateTestCaseTracesResponseWrapper(
            new List<TraceLinksRoot> { traceLinksRoot });

        _mockClient
            .Setup(c => c.GetTestCaseTracesV2("TEST-1", false))
            .ReturnsAsync(tracesResponse);

        // Act
        var result = await _testCaseAdditionalLinksService.GetAdditionalLinks(zephyrTestCase);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetAdditionalLinks_WithEmptyTraceLinks_ReturnsEmptyList()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1");
        var traceLinksRoot = TestDataHelper.CreateTraceLinksRoot(new List<TraceLink>(), id: 1);
        var tracesResponse = TestDataHelper.CreateTestCaseTracesResponseWrapper(
            new List<TraceLinksRoot> { traceLinksRoot });

        _mockClient
            .Setup(c => c.GetTestCaseTracesV2("TEST-1", false))
            .ReturnsAsync(tracesResponse);

        // Act
        var result = await _testCaseAdditionalLinksService.GetAdditionalLinks(zephyrTestCase);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetAdditionalLinks_WhenGetTestCaseTracesV2ReturnsNull_FallsBackToGetTestCaseTraces()
    {
        // Arrange
        var keyName = "TEST-1";
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: keyName);
        var traceLinksRoot = TestDataHelper.CreateTraceLinksRoot(
            new List<TraceLink> { TestDataHelper.CreateTraceLink(urlDescription: "Web Link", url: "https://example.com") }, id: 1);

        _mockClient
            .Setup(c => c.GetTestCaseTracesV2(keyName, false))
            .ReturnsAsync((TestCaseTracesResponseWrapper?)null);
        _mockClient
            .Setup(c => c.GetTestCaseTraces(keyName))
            .ReturnsAsync(traceLinksRoot);

        // Act
        var result = await _testCaseAdditionalLinksService.GetAdditionalLinks(zephyrTestCase);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Title, Is.EqualTo("Web Link"));
        });

        _mockClient.Verify(c => c.GetTestCaseTraces("TEST-1"), Times.Once);
    }

    [Test]
    public async Task GetAdditionalLinks_WhenTlRootIdDiffersFromJiraId_UpdatesJiraId()
    {
        // Arrange
        var keyName = "TEST-1";
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: keyName, jiraId: "99999");
        var traceLinksRoot = TestDataHelper.CreateTraceLinksRoot(new List<TraceLink>(), id: 10001);
        var tracesResponse = TestDataHelper.CreateTestCaseTracesResponseWrapper(
            new List<TraceLinksRoot> { traceLinksRoot });

        _mockClient
            .Setup(c => c.GetTestCaseTracesV2(keyName, false))
            .ReturnsAsync(tracesResponse);

        // Act
        await _testCaseAdditionalLinksService.GetAdditionalLinks(zephyrTestCase);

        // Assert
        Assert.That(zephyrTestCase.JiraId, Is.EqualTo("10001"));
    }

    [Test]
    public async Task GetAdditionalLinks_WhenClientThrowsException_ReturnsEmptyList()
    {
        // Arrange
        var keyName = "TEST-1";
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: keyName);
        var exception = new Exception("Client error");

        _mockClient
            .Setup(c => c.GetTestCaseTracesV2(keyName, false))
            .ThrowsAsync(exception);

        // Act
        var result = await _testCaseAdditionalLinksService.GetAdditionalLinks(zephyrTestCase);

        // Assert
        Assert.That(result, Is.Empty);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Getting additional links failed")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetAdditionalLinks_WithOnlyWebLinks_ReturnsWebLinks()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1");
        var traceLinks = new List<TraceLink>
        {
            TestDataHelper.CreateTraceLink(urlDescription: "Link1", url: "https://example.com/1"),
            TestDataHelper.CreateTraceLink(urlDescription: "Link2", url: "https://example.com/2")
        };
        var traceLinksRoot = TestDataHelper.CreateTraceLinksRoot(traceLinks, id: 1);
        var tracesResponse = TestDataHelper.CreateTestCaseTracesResponseWrapper(
            new List<TraceLinksRoot> { traceLinksRoot });

        _mockClient
            .Setup(c => c.GetTestCaseTracesV2("TEST-1", false))
            .ReturnsAsync(tracesResponse);

        // Act
        var result = await _testCaseAdditionalLinksService.GetAdditionalLinks(zephyrTestCase);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.All(l => l.Url.StartsWith("https://example.com")), Is.True);
        });

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Converting web links")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetAdditionalLinks_WithOnlyConfluenceLinks_ReturnsConfluenceLinks()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1");
        var traceLinks = new List<TraceLink>
        {
            TestDataHelper.CreateTraceLink(confluencePageId: "12345"),
            TestDataHelper.CreateTraceLink(confluencePageId: "67890")
        };
        var traceLinksRoot = TestDataHelper.CreateTraceLinksRoot(traceLinks, id: 1);
        var tracesResponse = TestDataHelper.CreateTestCaseTracesResponseWrapper(
            new List<TraceLinksRoot> { traceLinksRoot });

        var confluenceLinks1 = new List<ZephyrConfluenceLink>
        {
            TestDataHelper.CreateZephyrConfluenceLink("Page1", "https://confluence.com/page1")
        };
        var confluenceLinks2 = new List<ZephyrConfluenceLink>
        {
            TestDataHelper.CreateZephyrConfluenceLink("Page2", "https://confluence.com/page2")
        };

        _mockClient
            .Setup(c => c.GetTestCaseTracesV2("TEST-1", false))
            .ReturnsAsync(tracesResponse);

        _mockClient
            .Setup(c => c.GetConfluenceLinksByConfluencePageId("12345"))
            .ReturnsAsync(confluenceLinks1);

        _mockClient
            .Setup(c => c.GetConfluenceLinksByConfluencePageId("67890"))
            .ReturnsAsync(confluenceLinks2);

        // Act
        var result = await _testCaseAdditionalLinksService.GetAdditionalLinks(zephyrTestCase);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.Any(l => l.Title == "Page1"), Is.True);
            Assert.That(result.Any(l => l.Title == "Page2"), Is.True);
        });
    }

    [Test]
    public async Task GetAdditionalLinks_WithOnlyIssueLinks_ReturnsIssueLinks()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1");
        var traceLinks = new List<TraceLink>
        {
            TestDataHelper.CreateTraceLink(issueId: "10001"),
            TestDataHelper.CreateTraceLink(issueId: "10002")
        };
        var traceLinksRoot = TestDataHelper.CreateTraceLinksRoot(traceLinks, id: 1);
        var tracesResponse = TestDataHelper.CreateTestCaseTracesResponseWrapper(
            new List<TraceLinksRoot> { traceLinksRoot });

        var issue1 = TestDataHelper.CreateJiraIssue("TEST-1", "10001", "Issue 1");
        var issue2 = TestDataHelper.CreateJiraIssue("TEST-2", "10002", "Issue 2");

        _mockClient
            .Setup(c => c.GetTestCaseTracesV2("TEST-1", false))
            .ReturnsAsync(tracesResponse);

        _mockClient
            .Setup(c => c.GetIssueById("10001"))
            .ReturnsAsync(issue1);

        _mockClient
            .Setup(c => c.GetIssueById("10002"))
            .ReturnsAsync(issue2);

        // Act
        var result = await _testCaseAdditionalLinksService.GetAdditionalLinks(zephyrTestCase);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.Any(l => l.Title == "Issue 1" && l.Url.Contains("TEST-1")), Is.True);
            Assert.That(result.Any(l => l.Title == "Issue 2" && l.Url.Contains("TEST-2")), Is.True);
        });
    }

    [Test]
    public async Task GetAdditionalLinks_WithArchivedTestCase_UsesIsArchivedFlag()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1", isArchived: true);
        var traceLinksRoot = TestDataHelper.CreateTraceLinksRoot(new List<TraceLink>(), id: 1);
        var tracesResponse = TestDataHelper.CreateTestCaseTracesResponseWrapper(
            new List<TraceLinksRoot> { traceLinksRoot });

        _mockClient
            .Setup(c => c.GetTestCaseTracesV2("TEST-1", true))
            .ReturnsAsync(tracesResponse);

        // Act
        await _testCaseAdditionalLinksService.GetAdditionalLinks(zephyrTestCase);

        // Assert
        _mockClient.Verify(c => c.GetTestCaseTracesV2("TEST-1", true), Times.Once);
    }

    [Test]
    public async Task GetAdditionalLinks_WithConfluenceException_ContinuesProcessing()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1");
        var traceLinks = new List<TraceLink>
        {
            TestDataHelper.CreateTraceLink(confluencePageId: "12345"),
            TestDataHelper.CreateTraceLink(urlDescription: "Web Link", url: "https://example.com")
        };
        var traceLinksRoot = TestDataHelper.CreateTraceLinksRoot(traceLinks, id: 1);
        var tracesResponse = TestDataHelper.CreateTestCaseTracesResponseWrapper(
            new List<TraceLinksRoot> { traceLinksRoot });

        _mockClient
            .Setup(c => c.GetTestCaseTracesV2("TEST-1", false))
            .ReturnsAsync(tracesResponse);

        _mockClient
            .Setup(c => c.GetConfluenceLinksByConfluencePageId("12345"))
            .ThrowsAsync(new Exception("Confluence error"));

        // Act
        var result = await _testCaseAdditionalLinksService.GetAdditionalLinks(zephyrTestCase);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Title, Is.EqualTo("Web Link"));
        });
    }

    #endregion

    #region ConvertIssueLinkByIssueId

    [Test]
    public async Task ConvertIssueLinkByIssueId_WithValidIssueId_ReturnsLink()
    {
        // Arrange
        var issueId = "10001";
        var jiraIssue = TestDataHelper.CreateJiraIssue("TEST-123", issueId, "Test Issue Title");

        _mockClient
            .Setup(c => c.GetIssueById(issueId))
            .ReturnsAsync(jiraIssue);

        // Act
        var result = await _testCaseAdditionalLinksService.ConvertIssueLinkByIssueId(issueId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Title, Is.EqualTo("Test Issue Title"));
            Assert.That(result.Url, Is.EqualTo("https://jira.example.com/browse/TEST-123"));
        });

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Converting issue link")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void ConvertIssueLinkByIssueId_WithNullIssueId_ThrowsException()
    {
        // Arrange
        _mockClient
            .Setup(c => c.GetIssueById(null!))
            .ThrowsAsync(new ArgumentNullException("issueId"));

        // Act & Assert
        Assert.That(
            async () => await _testCaseAdditionalLinksService.ConvertIssueLinkByIssueId(null!),
            Throws.Exception);
    }

    [Test]
    public void ConvertIssueLinkByIssueId_WithEmptyIssueId_ThrowsException()
    {
        // Arrange
        _mockClient
            .Setup(c => c.GetIssueById(""))
            .ThrowsAsync(new ArgumentException());

        // Act & Assert
        Assert.That(
            async () => await _testCaseAdditionalLinksService.ConvertIssueLinkByIssueId(""),
            Throws.Exception);
    }

    [Test]
    public void ConvertIssueLinkByIssueId_WithClientException_PropagatesException()
    {
        // Arrange
        var issueId = "10001";
        var exception = new Exception("Client error");

        _mockClient
            .Setup(c => c.GetIssueById(issueId))
            .ThrowsAsync(exception);

        // Act & Assert
        Assert.That(
            async () => await _testCaseAdditionalLinksService.ConvertIssueLinkByIssueId(issueId),
            Throws.Exception.EqualTo(exception));
    }

    [Test]
    public async Task ConvertIssueLinkByIssueId_WithBaseUrlHavingTrailingSlash_TrimsSlash()
    {
        // Arrange
        var issueId = "10001";
        var jiraIssue = TestDataHelper.CreateJiraIssue("TEST-123", issueId, "Test Issue");
        _mockClient.Setup(c => c.GetBaseUrl()).Returns(new Uri("https://jira.example.com/"));

        _mockClient
            .Setup(c => c.GetIssueById(issueId))
            .ReturnsAsync(jiraIssue);

        // Act
        var result = await _testCaseAdditionalLinksService.ConvertIssueLinkByIssueId(issueId);

        // Assert
        Assert.That(result.Url, Is.EqualTo("https://jira.example.com/browse/TEST-123"));
    }

    #endregion
}
