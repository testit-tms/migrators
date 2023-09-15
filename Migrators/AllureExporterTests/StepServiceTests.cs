using AllureExporter.Client;
using AllureExporter.Models;
using AllureExporter.Services;
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
            await service.ConvertSteps(TestCaseId));
    }

    [Test]
    public async Task ConvertSteps_GetStepsSuccess()
    {
        // Arrange
        _client.GetSteps(Arg.Any<int>()).Returns(_allureSteps);

        var service = new StepService(_logger, _client);

        // Act
        var steps= await service.ConvertSteps(TestCaseId);

        // Assert
        Assert.AreEqual(3, steps.Count);
        Assert.AreEqual("<p>When</p>\n<p>Test step 1</p>\n<p>Test step 1.1</p>\n<p>And</p>\n<p>Test step 1.2</p>\n", steps[0].Action);
        Assert.AreEqual("Expected result", steps[0].Expected);
        Assert.AreEqual(3, steps[0].Attachments.Count);
        Assert.AreEqual("image.png", steps[0].Attachments[0]);
        Assert.AreEqual("image2.png", steps[0].Attachments[1]);
        Assert.AreEqual("image3.png", steps[0].Attachments[2]);
        Assert.AreEqual("<p></p>\n", steps[1].Action);
        Assert.IsNull(steps[1].Expected);
        Assert.AreEqual(0, steps[1].Attachments.Count);
        Assert.AreEqual("<p>Test step 3</p>\n", steps[2].Action);
        Assert.IsNull(steps[2].Expected);
        Assert.AreEqual(0, steps[2].Attachments.Count);
    }
}
