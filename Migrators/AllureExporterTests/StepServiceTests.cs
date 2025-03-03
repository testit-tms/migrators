using AllureExporter.Client;
using AllureExporter.Models;
using AllureExporter.Services;
using AllureExporter.Services.Implementations;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AllureExporterTests;

public class StepServiceTests
{
    private ILogger<StepService> _logger;
    private IClient _client;
    private const int TestCaseId = 1;
    private List<AllureStep> _allureSteps;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<StepService>>();
        _client = Substitute.For<IClient>();

        _allureSteps = new List<AllureStep>
        {
            new()
            {
                Keyword = "When",
                Name = "Test step 1",
                ExpectedResult = "Expected result",
                Attachments = new List<AllureAttachment>
                {
                    new()
                    {
                        Name = "image.png"
                    },
                    new()
                    {
                        Name = "image2.png"
                    }
                },
                Steps = new List<AllureStep>()
                {
                    new ()
                    {
                        Name = "Test step 1.1",
                        ExpectedResult = "Expected result 1.1",
                        Attachments = new List<AllureAttachment>
                        {
                            new()
                            {
                                Name = "image3.png"
                            }
                        },
                    },
                    new ()
                    {
                        Keyword = "And",
                        Name = "Test step 1.2",
                        ExpectedResult = "Expected result 1.2",
                        Attachments = new List<AllureAttachment>()
                    }
                }
            },
            new()
            {
                Name = string.Empty,
                Steps = new List<AllureStep>(),
                Attachments = new List<AllureAttachment>()
            },
            new ()
            {
                Name = "Test step 3",
                Steps = new List<AllureStep>(),
                Attachments = new List<AllureAttachment>()
            }
        };
    }

    [Test]
    public async Task ConvertSteps_FailedGetSteps()
    {
        // Arrange
        _client.GetSteps(Arg.Any<int>()).ThrowsAsync(new Exception("Failed to get steps"));

        var service = new StepService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ConvertStepsForTestCase(TestCaseId, []));
    }

    [Test]
    public async Task ConvertSteps_GetStepsSuccess()
    {
        // Arrange
        _client.GetSteps(Arg.Any<int>()).Returns(_allureSteps);

        var service = new StepService(_logger, _client);

        // Act
        var steps= await service.ConvertStepsForTestCase(TestCaseId, []);

        // Assert
        Assert.That(steps.Count, Is.EqualTo(3));
        Assert.That(steps[0].Action, Is.EqualTo("<p>When</p>\n<p>Test step 1</p>\n<p>Test step 1.1</p>\n<p>And</p>\n<p>Test step 1.2</p>\n"));
        Assert.That(steps[0].Expected, Is.EqualTo("Expected result"));
        Assert.That(steps[0].ActionAttachments.Count, Is.EqualTo(3));
        Assert.That(steps[0].ActionAttachments[0], Is.EqualTo("image.png"));
        Assert.That(steps[0].ActionAttachments[1], Is.EqualTo("image2.png"));
        Assert.That(steps[0].ActionAttachments[2], Is.EqualTo("image3.png"));
        Assert.That(steps[1].Action, Is.EqualTo("<p></p>\n"));
        Assert.IsNull(steps[1].Expected);
        Assert.That(steps[1].ActionAttachments.Count, Is.EqualTo(0));
        Assert.That(steps[2].Action, Is.EqualTo("<p>Test step 3</p>\n"));
        Assert.IsNull(steps[2].Expected);
        Assert.That(steps[2].ActionAttachments.Count, Is.EqualTo(0));
    }
}
