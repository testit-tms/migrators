using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ZephyrScaleExporter.Client;
using ZephyrScaleExporter.Models;
using ZephyrScaleExporter.Services;

namespace ZephyrScaleExporterTests;

public class StepServiceTests
{
    private ILogger<StepService> _logger;
    private IClient _client;
    private IAttachmentService _attachmentService;
    private List<ZephyrStep> _steps;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<StepService>>();
        _client = Substitute.For<IClient>();
        _attachmentService = Substitute.For<IAttachmentService>();

        _steps = new List<ZephyrStep>
        {
            new()
            {
                Inline = new Inline
                {
                    CustomFields = null,
                    Description =
                        "<img src=\"https://example.test/picture.jpg\" style=\"width:300px\" class=\"fr-fil fr-dib\" /> Action",
                    ExpectedResult =
                        "<img src=\"https://example.test/picture.jpg\" style=\"width:300px\" class=\"fr-fil fr-dib\" /> Expected",
                    TestData =
                        "<img src=\"https://example.test/picture.jpg\" style=\"width:300px\" class=\"fr-fil fr-dib\" /> Test Data"
                }
            }
        };
    }

    [Test]
    public async Task ConvertSteps_FailedGetSteps()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var testCaseName = "Test Case Name";
        var testScript = "teststeps";

        _client.GetSteps(testCaseName).Throws(new Exception("Failed to get steps"));

        var stepService = new StepService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await stepService.ConvertSteps(testCaseId, testCaseName, testScript));

        // Assert
        await _client.DidNotReceive()
            .GetTestScript(Arg.Any<string>());

        await _attachmentService.DidNotReceive()
            .DownloadAttachment(Arg.Any<Guid>(), Arg.Any<ZephyrAttachment>());
    }

    [Test]
    public async Task ConvertSteps_FailedGetTestScript()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var testCaseName = "Test Case Name";
        var testScript = "testscript";

        _client.GetTestScript(testCaseName).Throws(new Exception("Failed to get test script"));

        var stepService = new StepService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await stepService.ConvertSteps(testCaseId, testCaseName, testScript));

        // Assert
        await _client.DidNotReceive()
            .GetSteps(Arg.Any<string>());

        await _attachmentService.DidNotReceive()
            .DownloadAttachment(Arg.Any<Guid>(), Arg.Any<ZephyrAttachment>());
    }

    [Test]
    public async Task ConvertSteps_FailedDownloadAttachment()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var testCaseName = "Test Case Name";
        var testScript = "teststeps";

        _client.GetSteps(testCaseName)
            .Returns(_steps);

        _attachmentService.DownloadAttachment(testCaseId, Arg.Any<ZephyrAttachment>())
            .Throws(new Exception("Failed to download attachment"));

        var stepService = new StepService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await stepService.ConvertSteps(testCaseId, testCaseName, testScript));

        // Assert
        await _client.DidNotReceive()
            .GetTestScript(Arg.Any<string>());
    }

    [Test]
    public async Task ConvertSteps_Success()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var testCaseName = "Test Case Name";
        var testScript = "teststeps";

        _client.GetSteps(testCaseName)
            .Returns(_steps);

        _attachmentService.DownloadAttachment(testCaseId, Arg.Any<ZephyrAttachment>())
            .Returns("picture.jpg");

        var stepService = new StepService(_logger, _client, _attachmentService);

        // Act
        var steps = await stepService.ConvertSteps(testCaseId, testCaseName, testScript);

        // Assert
        Assert.That(steps, Has.Count.EqualTo(1));
        Assert.That(steps[0].Action, Is.EqualTo("<<<picture.jpg>>> Action"));
        Assert.That(steps[0].Expected, Is.EqualTo("<<<picture.jpg>>> Expected"));
        Assert.That(steps[0].TestData, Is.EqualTo("<<<picture.jpg>>> Test Data<br><p></p>"));
        Assert.That(steps[0].ActionAttachments, Has.Count.EqualTo(1));
        Assert.That(steps[0].ActionAttachments[0], Is.EqualTo("picture.jpg"));
        Assert.That(steps[0].ExpectedAttachments, Has.Count.EqualTo(1));
        Assert.That(steps[0].ExpectedAttachments[0], Is.EqualTo("picture.jpg"));
        Assert.That(steps[0].TestDataAttachments, Has.Count.EqualTo(1));
        Assert.That(steps[0].TestDataAttachments[0], Is.EqualTo("picture.jpg"));
    }

    [Test]
    public async Task ConvertSteps_GetTestScript_Success()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var testCaseName = "Test Case Name";
        var testScript = "testscript";

        _client.GetTestScript(testCaseName)
            .Returns(new ZephyrTestScript
            {
                Text = "teststeps"
            });

        var stepService = new StepService(_logger, _client, _attachmentService);

        // Act
        var result = await stepService.ConvertSteps(testCaseId, testCaseName, testScript);

        // Assert
        await _client.DidNotReceive()
            .GetSteps(Arg.Any<string>());

        await _attachmentService.DidNotReceive()
            .DownloadAttachment(Arg.Any<Guid>(), Arg.Any<ZephyrAttachment>());

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Action, Is.EqualTo("teststeps"));
        Assert.That(result[0].Expected, Is.Empty);
        Assert.That(result[0].TestData, Is.Empty);
        Assert.That(result[0].ActionAttachments, Has.Count.EqualTo(0));
        Assert.That(result[0].ExpectedAttachments, Has.Count.EqualTo(0));
        Assert.That(result[0].TestDataAttachments, Has.Count.EqualTo(0));
    }
}
