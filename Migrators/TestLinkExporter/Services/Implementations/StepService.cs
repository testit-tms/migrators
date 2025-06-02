using Microsoft.Extensions.Logging;
using Models;
using TestLinkExporter.Models.Step;

namespace TestLinkExporter.Services.Implementations;

public class StepService : IStepService
{
    private readonly ILogger<StepService> _logger;

    public StepService(ILogger<StepService> logger)
    {
        _logger = logger;
    }

    public List<Step> ConvertSteps(List<TestLinkStep> testLinkSteps)
    {
        _logger.LogDebug("Found steps: {@Steps}", testLinkSteps);

        var steps = new List<Step>();

        foreach (var testLinkStep in testLinkSteps)
        {
            steps.Add(
            new Step
            {
                Action = testLinkStep.Actions,
                Expected = testLinkStep.ExpectedResult,
                ActionAttachments = new List<string>(),
                ExpectedAttachments = new List<string>(),
                TestDataAttachments = new List<string>(),
            }
        );
        }
        _logger.LogDebug("Converted steps: {@Steps}", steps);

        return steps;
    }
}
