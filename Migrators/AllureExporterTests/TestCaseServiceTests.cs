using AllureExporter.Client;
using AllureExporter.Models;
using AllureExporter.Services;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Exception = System.Exception;

namespace AllureExporterTests;

public class TestCaseServiceTests
{
    private ILogger<TestCaseService> _logger;
    private IClient _client;
    private IAttachmentService _attachmentService;
    private const int ProjectId = 1;
    private readonly Guid _statusAttribute = Guid.NewGuid();
    private readonly Guid _layerAttribute = Guid.NewGuid();

    private readonly Dictionary<int, Guid> _sectionIdMap = new()
    {
        { 0, Guid.NewGuid() },
        { 1, Guid.NewGuid() }
    };

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<TestCaseService>>();
        _client = Substitute.For<IClient>();
        _attachmentService = Substitute.For<IAttachmentService>();
    }

    [Test]
    public async Task ConvertTestCases_FailedGetTestCaseIdsFromMainSuite()
    {
        // Arrange
        _client.GetTestCaseIdsFromMainSuite(ProjectId)
            .ThrowsAsync(new Exception("Failed to get test case ids from main suite"));

        var service = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ConvertTestCases(ProjectId, _statusAttribute, _layerAttribute, _sectionIdMap));

        // Assert
        await _client.DidNotReceive()
            .GetTestCaseIdsFromSuite(ProjectId, Arg.Any<int>());
        await _attachmentService.DidNotReceive()
            .DownloadAttachments(Arg.Any<Guid>(), Arg.Any<List<AllureAttachment>>());
        await _client.DidNotReceive().GetTestCaseById(Arg.Any<int>());
        await _client.DidNotReceive().GetLinks(Arg.Any<int>());
        await _client.DidNotReceive().DownloadAttachment(Arg.Any<int>());
        await _client.DidNotReceive().GetSteps(Arg.Any<int>());
    }

    [Test]
    public async Task ConvertTestCases_FailedGetTestCaseIdsFromSuite()
    {
        // Arrange
        _client.GetTestCaseIdsFromSuite(ProjectId, 1)
            .ThrowsAsync(new Exception("Failed to get test case ids from suite"));

        var service = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ConvertTestCases(ProjectId, _statusAttribute, _layerAttribute, new Dictionary<int, Guid>()
            {
                { 1, _sectionIdMap[1] }
            }));

        // Assert
        await _client.DidNotReceive()
            .GetTestCaseIdsFromMainSuite(ProjectId);
        await _attachmentService.DidNotReceive()
            .DownloadAttachments(Arg.Any<Guid>(), Arg.Any<List<AllureAttachment>>());
        await _client.DidNotReceive().GetTestCaseById(Arg.Any<int>());
        await _client.DidNotReceive().GetLinks(Arg.Any<int>());
        await _client.DidNotReceive().DownloadAttachment(Arg.Any<int>());
        await _client.DidNotReceive().GetSteps(Arg.Any<int>());
    }

    [Test]
    public async Task ConvertTestCases_FailedGetTestCaseById()
    {
        // Arrange
        _client.GetTestCaseIdsFromSuite(ProjectId, 1)
            .Returns(new List<int> { 1, 2 });
        _client.GetTestCaseById(1)
            .ThrowsAsync(new Exception("Failed to get test case by id"));

        var service = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ConvertTestCases(ProjectId, _statusAttribute, _layerAttribute, new Dictionary<int, Guid>
            {
                { 1, _sectionIdMap[1] }
            }));

        // Assert
        await _client.DidNotReceive()
            .GetTestCaseIdsFromMainSuite(ProjectId);
        await _attachmentService.DidNotReceive()
            .DownloadAttachments(Arg.Any<Guid>(), Arg.Any<List<AllureAttachment>>());
        await _client.DidNotReceive().GetLinks(Arg.Any<int>());
        await _client.DidNotReceive().DownloadAttachment(Arg.Any<int>());
        await _client.DidNotReceive().GetSteps(Arg.Any<int>());
    }

    [Test]
    public async Task ConvertTestCases_FailedDownloadAttachment()
    {
        // Arrange
        _client.GetTestCaseIdsFromSuite(ProjectId, 1)
            .Returns(new List<int> { 1, 2 });
        _client.GetTestCaseById(1)
            .Returns(new AllureTestCase());
        _client.GetAttachments(1)
            .ThrowsAsync(new Exception("Failed to get attachments"));

        var service = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ConvertTestCases(ProjectId, _statusAttribute, _layerAttribute, new Dictionary<int, Guid>
            {
                { 1, _sectionIdMap[1] }
            }));

        // Assert
        await _client.DidNotReceive()
            .GetTestCaseIdsFromMainSuite(ProjectId);
        await _attachmentService.DidNotReceive()
            .DownloadAttachments(Arg.Any<Guid>(), Arg.Any<List<AllureAttachment>>());
        await _client.DidNotReceive().GetLinks(Arg.Any<int>());
        await _client.DidNotReceive().GetSteps(Arg.Any<int>());
    }

    [Test]
    public async Task ConvertTestCases_FailedGetLinks()
    {
        // Arrange
        _client.GetTestCaseIdsFromSuite(ProjectId, 1)
            .Returns(new List<int> { 1, 2 });
        _client.GetTestCaseById(1)
            .Returns(new AllureTestCase());
        _client.GetAttachments(1)
            .Returns(new List<AllureAttachment>());
        _client.GetLinks(1).ThrowsAsync(new Exception("Failed to get links"));

        var service = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ConvertTestCases(ProjectId, _statusAttribute, _layerAttribute, new Dictionary<int, Guid>
            {
                { 1, _sectionIdMap[1] }
            }));

        // Assert
        await _client.DidNotReceive()
            .GetTestCaseIdsFromMainSuite(ProjectId);
        await _attachmentService.DidNotReceive()
            .DownloadAttachments(Arg.Any<Guid>(), Arg.Any<List<AllureAttachment>>());
        await _client.DidNotReceive().GetSteps(Arg.Any<int>());
    }

    [Test]
    public async Task ConvertTestCases_FailedDownloadAttachments()
    {
        // Arrange
        _client.GetTestCaseIdsFromSuite(ProjectId, 1)
            .Returns(new List<int> { 1, 2 });
        _client.GetTestCaseById(1)
            .Returns(new AllureTestCase());
        _client.GetAttachments(1)
            .Returns(new List<AllureAttachment>());
        _client.GetLinks(1).Returns(new List<AllureLink>());
        _attachmentService.DownloadAttachments(Arg.Any<Guid>(), Arg.Any<List<AllureAttachment>>())
            .ThrowsAsync(new Exception("Failed to download attachments"));

        var service = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ConvertTestCases(ProjectId, _statusAttribute, _layerAttribute, new Dictionary<int, Guid>
            {
                { 1, _sectionIdMap[1] }
            }));

        // Assert
        await _client.DidNotReceive()
            .GetTestCaseIdsFromMainSuite(ProjectId);
        await _client.DidNotReceive().GetSteps(Arg.Any<int>());
    }

    [Test]
    public async Task ConvertTestCases_FailedGetSteps()
    {
        // Arrange
        _client.GetTestCaseIdsFromSuite(ProjectId, 1)
            .Returns(new List<int> { 1, 2 });
        _client.GetTestCaseById(1)
            .Returns(new AllureTestCase());
        _client.GetAttachments(1)
            .Returns(new List<AllureAttachment>());
        _client.GetLinks(1).Returns(new List<AllureLink>());
        _attachmentService.DownloadAttachments(Arg.Any<Guid>(), Arg.Any<List<AllureAttachment>>())
            .Returns(new List<string>());
        _client.GetSteps(1).ThrowsAsync(new Exception("Failed to get steps"));

        var service = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ConvertTestCases(ProjectId, _statusAttribute, _layerAttribute, new Dictionary<int, Guid>
            {
                { 1, _sectionIdMap[1] }
            }));

        // Assert
        await _client.DidNotReceive()
            .GetTestCaseIdsFromMainSuite(ProjectId);
    }

    [Test]
    public async Task ConvertTestCases_Success()
    {
        // Arrange
        _client.GetTestCaseIdsFromSuite(ProjectId, 1)
            .Returns(new List<int> { 1 });
        _client.GetTestCaseById(1)
            .Returns(new AllureTestCase
            {
                Description = "Test description",
                Id = 1,
                Name = "Test name",
                Status = new Status
                {
                    Name = "Ready"
                },
                Tags = new List<Tags>
                {
                    new()
                    {
                        Name = "Test tag"
                    }
                },
                Layer = new TestLayer()
                {
                    Name = "Unit Tests"
                }
            });
        _client.GetAttachments(1)
            .Returns(new List<AllureAttachment>());
        _client.GetLinks(1).Returns(new List<AllureLink>());
        _attachmentService.DownloadAttachments(Arg.Any<Guid>(), Arg.Any<List<AllureAttachment>>())
            .Returns(new List<string>());
        _client.GetSteps(1).Returns(new List<AllureStep>
        {
            new()
            {
                Keyword = "Given",
                Name = "Test step 1",
                Steps = new List<AllureStep>
                {
                    new()
                    {
                        Keyword = "When",
                        Name = "Test step 2",
                        Attachments = new List<AllureAttachment>(),
                        Steps = new List<AllureStep>()
                    }
                },
                Attachments = new List<AllureAttachment>()
            }
        });

        var service = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        var testcases = await service.ConvertTestCases(ProjectId, _statusAttribute, _layerAttribute, new Dictionary<int, Guid>
        {
            { 1, _sectionIdMap[1] }
        });

        // Assert
        await _client.DidNotReceive()
            .GetTestCaseIdsFromMainSuite(ProjectId);
        Assert.That(testcases[0].Name, Is.EqualTo("Test name"));
        Assert.That(testcases[0].Description, Is.EqualTo("Test description"));
        Assert.That(testcases[0].State, Is.EqualTo(StateType.NotReady));
        Assert.That(testcases[0].Priority, Is.EqualTo(PriorityType.Medium));
        Assert.That(testcases[0].Tags, Is.EqualTo(new List<string> { "Test tag" }));
        Assert.That(testcases[0].Steps[0].Action, Is.EqualTo("<p>Given</p>\n<p>Test step 1</p>\n<p>When</p>\n<p>Test step 2</p>\n"));
        Assert.That(testcases[0].Attributes[0].Value, Is.EqualTo("Ready"));
        Assert.That(testcases[0].Attributes[1].Value, Is.EqualTo("Unit Tests"));
        Assert.That(testcases[0].SectionId, Is.EqualTo(_sectionIdMap[1]));
    }
}
