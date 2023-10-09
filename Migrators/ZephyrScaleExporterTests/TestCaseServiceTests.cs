using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ZephyrScaleExporter.Client;
using ZephyrScaleExporter.Models;
using ZephyrScaleExporter.Services;
using Constants = ZephyrScaleExporter.Models.Constants;

namespace ZephyrScaleExporterTests;

public class TestCaseServiceTests
{
    private ILogger<TestCaseService> _logger;
    private IClient _client;
    private IStepService _stepService;
    private IAttachmentService _attachmentService;
    private Dictionary<int, Guid> _sectionMap;
    private List<ZephyrTestCase> _zephyrTestCases;
    private List<Step> _steps;
    private Dictionary<string, Guid> _attributeMap;
    private Dictionary<int, string> _statusMap;
    private Dictionary<int, string> _priorityMap;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<TestCaseService>>();
        _client = Substitute.For<IClient>();
        _stepService = Substitute.For<IStepService>();
        _attachmentService = Substitute.For<IAttachmentService>();

        _sectionMap = new Dictionary<int, Guid>
        {
            { 1, Guid.NewGuid() }
        };

        _zephyrTestCases = new List<ZephyrTestCase>
        {
            new()
            {
                Id = 1,
                CustomFields = new Dictionary<string, object>
                {
                    { "CustomField", 321 }
                },
                Description =
                    "<img src=\"https://example.test/picture.jpg\" style=\"width:300px\" class=\"fr-fil fr-dib\" />some data 01",
                Labels = new List<string>(),
                Priority = new Priority
                {
                    Id = 123
                },
                Status = new Status
                {
                    Id = 321
                },
                Precondition =
                    "<img src=\"https://example.test/picture.jpg\" style=\"width:300px\" class=\"fr-fil fr-dib\" />some data 01",
                Key = "Test",
                Name = "Test",
                Links = new Links
                {
                    Issues = new List<Issues>
                    {
                        new()
                        {
                            Target = "https://example.test/issue/1"
                        }
                    },
                    WebLinks = new List<WebLinks>
                    {
                        new ()
                        {
                            Description = "Test",
                            Url = "https://example.test"
                        }
                    }
                },
                TestScript = new TestScript
                {
                    Self = "teststeps"
                }
            }
        };

        _steps = new List<Step>
        {
            new()
            {
                Action = "Test",
                ActionAttachments = new List<string>(),
                Expected = "Test",
                ExpectedAttachments = new List<string>(),
                TestData = "Test",
                TestDataAttachments = new List<string>()
            }
        };

        _attributeMap = new Dictionary<string, Guid>
        {
            { Constants.StateAttribute, Guid.NewGuid() },
            { Constants.PriorityAttribute, Guid.NewGuid() },
        };

        _statusMap = new Dictionary<int, string>
        {
            { 321, "Not Executed" }
        };

        _priorityMap = new Dictionary<int, string>
        {
            { 123, "Low" }
        };
    }

    [Test]
    public async Task ConvertTestCases_FailedGetTestCases()
    {
        // Arrange
        _client.GetTestCases(_sectionMap.Keys.First())
            .Throws(new Exception("Failed to get test cases"));

        var testCaseService = new TestCaseService(_logger, _client, _stepService, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await testCaseService.ConvertTestCases(_sectionMap, new Dictionary<string, Guid>(),
                new Dictionary<int, string>(), new Dictionary<int, string>()));

        // Assert
        await _stepService.DidNotReceive()
            .ConvertSteps(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>());

        await _attachmentService.DidNotReceive()
            .DownloadAttachment(Arg.Any<Guid>(), Arg.Any<ZephyrAttachment>());
    }

    [Test]
    public async Task ConvertTestCases_FailedConvertSteps()
    {
        // Arrange
        _client.GetTestCases(_sectionMap.Keys.First())
            .Returns(_zephyrTestCases);

        _stepService.ConvertSteps(
                Arg.Any<Guid>(),
                _zephyrTestCases[0].Name,
                _zephyrTestCases[0].TestScript.Self
            )
            .Throws(new Exception("Failed to convert steps"));

        var testCaseService = new TestCaseService(_logger, _client, _stepService, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await testCaseService.ConvertTestCases(_sectionMap, new Dictionary<string, Guid>(),
                new Dictionary<int, string>(), new Dictionary<int, string>()));

        // Assert
        await _attachmentService.DidNotReceive()
            .DownloadAttachment(Arg.Any<Guid>(), Arg.Any<ZephyrAttachment>());
    }

    [Test]
    public async Task ConvertTestCases_FailedDownloadAttachment()
    {
        // Arrange
        _client.GetTestCases(_sectionMap.Keys.First())
            .Returns(_zephyrTestCases);

        _stepService.ConvertSteps(
                Arg.Any<Guid>(),
                _zephyrTestCases[0].Name,
                _zephyrTestCases[0].TestScript.Self
            )
            .Returns(_steps);

        _attachmentService.DownloadAttachment(Arg.Any<Guid>(), Arg.Any<ZephyrAttachment>())
            .Throws(new Exception("Failed to download attachment"));

        var testCaseService = new TestCaseService(_logger, _client, _stepService, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await testCaseService.ConvertTestCases(_sectionMap, new Dictionary<string, Guid>(),
                new Dictionary<int, string>(), new Dictionary<int, string>()));
    }

    [Test]
    public async Task ConvertTestCases_Success()
    {
        // Arrange
        _client.GetTestCases(_sectionMap.Keys.First())
            .Returns(_zephyrTestCases);

        _stepService.ConvertSteps(
                Arg.Any<Guid>(),
                _zephyrTestCases[0].Name,
                _zephyrTestCases[0].TestScript.Self
            )
            .Returns(_steps);

        _attachmentService.DownloadAttachment(Arg.Any<Guid>(), Arg.Any<ZephyrAttachment>())
            .Returns("picture.jpg");

        var testCaseService = new TestCaseService(_logger, _client, _stepService, _attachmentService);

        // Act
        var testCaseData = await testCaseService.ConvertTestCases(_sectionMap, _attributeMap,
            _statusMap, _priorityMap);

        // Assert
        Assert.That(testCaseData.TestCases, Has.Count.EqualTo(1));
        Assert.That(testCaseData.TestCases[0].Name, Is.EqualTo(_zephyrTestCases[0].Name));
        Assert.That(testCaseData.TestCases[0].Description, Is.EqualTo("<<<picture.jpg>>>some data 01"));
        Assert.That(testCaseData.TestCases[0].PreconditionSteps, Has.Count.EqualTo(1));
        Assert.That(testCaseData.TestCases[0].PreconditionSteps[0].Action, Is.EqualTo("<<<picture.jpg>>>some data 01"));
        Assert.That(testCaseData.TestCases[0].Priority, Is.EqualTo(PriorityType.Medium));
        Assert.That(testCaseData.TestCases[0].State, Is.EqualTo(StateType.NotReady));
        Assert.That(testCaseData.TestCases[0].Tags, Is.Empty);
        Assert.That(testCaseData.TestCases[0].Attributes, Has.Count.EqualTo(3));
        Assert.That(testCaseData.TestCases[0].Attributes[0].Id, Is.EqualTo(_attributeMap[Constants.StateAttribute]));
        Assert.That(testCaseData.TestCases[0].Attributes[0].Value, Is.EqualTo(_statusMap[321]));
        Assert.That(testCaseData.TestCases[0].Attributes[1].Id, Is.EqualTo(_attributeMap[Constants.PriorityAttribute]));
        Assert.That(testCaseData.TestCases[0].Attributes[1].Value, Is.EqualTo(_priorityMap[123]));
        Assert.That(testCaseData.TestCases[0].Steps, Has.Count.EqualTo(1));
        Assert.That(testCaseData.TestCases[0].Steps[0].Action, Is.EqualTo("Test"));
        Assert.That(testCaseData.TestCases[0].Steps[0].Expected, Is.EqualTo("Test"));
        Assert.That(testCaseData.TestCases[0].Steps[0].TestData, Is.EqualTo("Test"));
        Assert.That(testCaseData.TestCases[0].Steps[0].ActionAttachments, Is.Empty);
        Assert.That(testCaseData.TestCases[0].Steps[0].ExpectedAttachments, Is.Empty);
        Assert.That(testCaseData.TestCases[0].Steps[0].TestDataAttachments, Is.Empty);
        Assert.That(testCaseData.TestCases[0].Attachments, Has.Count.EqualTo(2));
        Assert.That(testCaseData.TestCases[0].Attachments[0], Is.EqualTo("picture.jpg"));
        Assert.That(testCaseData.TestCases[0].SectionId, Is.EqualTo(_sectionMap[1]));
        Assert.That(testCaseData.Attributes, Has.Count.EqualTo(1));
        Assert.That(testCaseData.TestCases[0].Links, Has.Count.EqualTo(2));
        Assert.That(testCaseData.TestCases[0].Links[0].Title, Is.EqualTo("Test"));
        Assert.That(testCaseData.TestCases[0].Links[0].Url, Is.EqualTo("https://example.test"));
        Assert.That(testCaseData.TestCases[0].Links[1].Title, Is.Null);
        Assert.That(testCaseData.TestCases[0].Links[1].Url, Is.EqualTo("https://example.test/issue/1"));
    }
}
