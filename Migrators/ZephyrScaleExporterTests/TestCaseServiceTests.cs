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
}
