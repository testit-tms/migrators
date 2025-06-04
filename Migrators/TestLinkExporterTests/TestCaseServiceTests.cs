using TestLinkExporter.Client;
using TestLinkExporter.Services;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Exception = System.Exception;
using TestLinkExporter.Models.Step;
using TestLinkExporter.Models.TestCase;
using TestLinkExporter.Models.Attachment;
using TestLinkExporter.Services.Implementations;
using Constants = TestLinkExporter.Models.Project.Constants;

namespace TestLinkExporterTests;

public class TestCaseServiceTests
{
    private ILogger<TestCaseService> _logger;
    private IClient _client;
    private IAttachmentService _attachmentService;
    private IStepService _stepService;
    private const int SuiteId = 1;
    private CaseAttribute _idAttribute;
    private Dictionary<string, Guid> _customAttributes;
    private List<string> _keywords;
    private List<TestLinkTestCase> _testCases;
    private List<TestLinkAttachment> _attachments;

    private readonly Dictionary<int, Guid> _sectionMap = new()
    {
        { SuiteId, Guid.NewGuid() }
    };

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<TestCaseService>>();
        _client = Substitute.For<IClient>();
        _attachmentService = Substitute.For<IAttachmentService>();
        _stepService = Substitute.For<IStepService>();
        _testCases = new List<TestLinkTestCase> {
            new TestLinkTestCase
            {
                ExternalId = "1",
                Summary = "Test description",
                Id = 1,
                Name = "Test name",
                Status = 1,
                Importance = 1,
                Preconditions = "Precon",
                Steps = new List<TestLinkStep>
                {
                    new()
                    {
                        Actions = "Test step 1",
                        ExpectedResult = "Expected result",
                    }
                }
            }
        };
        _keywords = new List<string> { "Keyword1", "Keyword2" };
        _idAttribute = new CaseAttribute
        {
            Id = Guid.NewGuid(),
            Value = Constants.TestLinkPrefixId + _testCases[0].ExternalId,
        };
        _customAttributes = new Dictionary<string, Guid>
        {
            { Constants.TestLinkId, _idAttribute.Id }
        };
        _attachments = new List<TestLinkAttachment>
        {
            new()
            {
                Content = new byte[] { 1, 2, 3 },
                Name = "TestAttachment1.png"
            },
            new()
            {
                Content = new byte[] { 1, 2, 3 },
                Name = "TestAttachment2.txt"
            }
        };
    }

    [Test]
    public async Task ConvertTestCases_FailedGetTestCaseIdsFromMainSuite()
    {
        // Arrange
        _client.GetTestCaseIdsBySuiteId(SuiteId)
            .Throws(new Exception("Failed to get test case ids from main suite"));

        var service = new TestCaseService(_logger, _client, _stepService, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await service.ConvertTestCases(_sectionMap, _customAttributes));

        // Assert
        _client.DidNotReceive().GetTestCaseById(Arg.Any<int>());
        _client.DidNotReceive().GetAttachmentsByTestCaseId(Arg.Any<int>());
        _attachmentService.DidNotReceive().DownloadAttachments(Arg.Any<int>(), Arg.Any<Guid>());
        _stepService.DidNotReceive().ConvertSteps(Arg.Any<List<TestLinkStep>>());
    }

    [Test]
    public async Task ConvertTestCases_FailedGetTestCaseById()
    {
        // Arrange
        _client.GetTestCaseIdsBySuiteId(SuiteId)
            .Returns(new List<int> { _testCases[0].Id });
        _client.GetTestCaseById(_testCases[0].Id)
            .Throws(new Exception("Failed to get test case by id"));

        var service = new TestCaseService(_logger, _client, _stepService, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await service.ConvertTestCases(_sectionMap, _customAttributes));

        // Assert
        _client.DidNotReceive().GetAttachmentsByTestCaseId(Arg.Any<int>());
        _attachmentService.DidNotReceive().DownloadAttachments(Arg.Any<int>(), Guid.NewGuid());
        _stepService.DidNotReceive().ConvertSteps(Arg.Any<List<TestLinkStep>>());
    }

    [Test]
    public async Task ConvertTestCases_FailedConvertSteps()
    {
        // Arrange
        _client.GetTestCaseIdsBySuiteId(SuiteId)
            .Returns(new List<int> { _testCases[0].Id });
        _client.GetTestCaseById(1)
            .Returns(_testCases[0]);
        _client.GetAttachmentsByTestCaseId(1)
            .Returns(_attachments);
        _attachmentService.DownloadAttachments(Arg.Any<int>(), Arg.Any<Guid>())
            .Returns(new List<string> { _attachments[0].Name, _attachments[1].Name });
        _stepService.ConvertSteps(_testCases[0].Steps).Throws(new Exception("Failed to convert steps"));

        var service = new TestCaseService(_logger, _client, _stepService, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await service.ConvertTestCases(new Dictionary<int, Guid>
            {
                { 1, _sectionMap[1] }
            },
            _customAttributes
        ));
    }

    [Test]
    public async Task ConvertTestCases_Success()
    {
        // Arrange
        _client.GetTestCaseIdsBySuiteId(SuiteId)
            .Returns(new List<int> { _testCases[0].Id });
        _client.GetTestCaseById(_testCases[0].Id)
            .Returns(_testCases[0]);
        _client.GetKeywordsByTestCaseById(_testCases[0].Id).Returns(_keywords);
        _client.GetAttachmentsByTestCaseId(_testCases[0].Id)
            .Returns(_attachments);
        _attachmentService.DownloadAttachments(Arg.Any<int>(), Arg.Any<Guid>())
            .Returns(_attachments.Select(a => a.Name).ToList());
        _stepService.ConvertSteps(_testCases[0].Steps).Returns(new List<Step>()
        {
            new()
            {
                Action = "Test step 1",
                Expected = "Expected result",
                ActionAttachments = new List<string>(),
            }
        });

        var service = new TestCaseService(_logger, _client, _stepService, _attachmentService);

        // Act
        var testcases = await service.ConvertTestCases(new Dictionary<int, Guid>
            {
                { 1, _sectionMap[1] }
            }, _customAttributes);

        // Assert
        Assert.That(testcases[0].Name, Is.EqualTo("Test name"));
        Assert.That(testcases[0].Description, Is.EqualTo("Test description"));
        Assert.That(testcases[0].State, Is.EqualTo(StateType.NotReady));
        Assert.That(testcases[0].Priority, Is.EqualTo(PriorityType.Low));
        Assert.That(testcases[0].Tags, Is.EqualTo(_keywords));
        Assert.That(testcases[0].Steps[0].Action, Is.EqualTo("Test step 1"));
        Assert.That(testcases[0].PreconditionSteps[0].Action, Is.EqualTo("Precon"));
        Assert.That(testcases[0].Attributes[0].Id, Is.EqualTo(_idAttribute.Id));
        Assert.That(testcases[0].Attributes[0].Value, Is.EqualTo(_idAttribute.Value));
        Assert.That(testcases[0].SectionId, Is.EqualTo(_sectionMap[1]));
        Assert.IsEmpty(testcases[0].Links);
    }
}
