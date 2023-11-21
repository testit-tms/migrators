using Microsoft.Extensions.Logging;
using Models;
using SpiraTestExporter.Client;

namespace SpiraTestExporter.Services;

public class StepService
{
    private readonly ILogger<StepService> _logger;
    private readonly IClient _client;

    public StepService(ILogger<StepService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<List<Step>> ConvertSteps(int projectId, int testCaseId)
    {
        _logger.LogInformation("Converting steps for test case {TestCaseId}", testCaseId);

        var steps = new List<Step>();
        var spiraSteps = await _client.GetTestSteps(projectId, testCaseId);

        foreach (var spiraStep in spiraSteps)
        {
            var stepParameters = await _client.GetStepParameters(projectId, testCaseId, spiraStep.Id);

            var step = new Step
            {
                Action = spiraStep.Description,
                Expected = spiraStep.ExpectedResult,
                TestData = stepParameters.Select(p => $"{p.Name}: {p.Value}").Aggregate((a, b) => $"{a}\n{b}"),
                ActionAttachments = new List<string>(),
                ExpectedAttachments = new List<string>(),
                TestDataAttachments = new List<string>()
            };

            steps.Add(step);
        }

        return steps;
    }
}
