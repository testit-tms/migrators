using Microsoft.Extensions.Logging;
using NSubstitute;
using TestLinkExporter.Models.Step;
using TestLinkExporter.Services.Implementations;

namespace TestLinkExporterTests;

public class StepServiceTests
{
    private ILogger<StepService> _logger;
    private List<TestLinkStep> _steps;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<StepService>>();

        _steps = new List<TestLinkStep>
        {
            new()
            {
                Actions = "Test step 1",
                ExpectedResult = "Expected result",
                StepNumber = 1,
            },
            new()
            {
                Actions = string.Empty,
                ExpectedResult = string.Empty,
                StepNumber = 2,
            },
            new ()
            {
                Actions = string.Empty,
                ExpectedResult = "Expected result",
                StepNumber = 3,
            }
        };
    }

    [Test]
    public async Task ConvertSteps_GetStepsSuccess()
    {
        // Arrange
        var service = new StepService(_logger);

        // Act
        var steps = service.ConvertSteps(_steps);

        // Assert
        Assert.AreEqual(3, steps.Count);
        Assert.AreEqual(_steps[0].Actions, steps[0].Action);
        Assert.AreEqual(_steps[0].ExpectedResult, steps[0].Expected);
        Assert.AreEqual(0, steps[0].ActionAttachments.Count);
        Assert.AreEqual(_steps[1].Actions, steps[1].Action);
        Assert.AreEqual(_steps[1].ExpectedResult, steps[1].Expected);
        Assert.AreEqual(0, steps[1].ActionAttachments.Count);
        Assert.AreEqual(_steps[2].Actions, steps[2].Action);
        Assert.AreEqual(_steps[2].ExpectedResult, steps[2].Expected);
        Assert.AreEqual(0, steps[2].ActionAttachments.Count);
    }
}
