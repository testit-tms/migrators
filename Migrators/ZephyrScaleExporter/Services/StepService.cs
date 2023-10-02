using Microsoft.Extensions.Logging;
using Models;
using ZephyrScaleExporter.Client;

namespace ZephyrScaleExporter.Services;

public class StepService : IStepService
{
    private readonly ILogger<StepService> _logger;
    private readonly IClient _client;

    public StepService(ILogger<StepService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<List<Step>> ConvertSteps(string testCaseName, string testScript)
    {
        _logger.LogInformation("Converting steps for test case {testCaseName}", testCaseName);

        if (testScript.Contains("teststeps"))
        {
            var steps = await _client.GetSteps(testCaseName);

            return steps.Select(step => new Step
            {
                Action = step.Inline.Description,
                Expected = step.Inline.ExpectedResult,
                TestData = step.Inline.TestData + "\n\n" + step.Inline.CustomFields,
                Attachments = new List<string>()
            }).ToList();
        }

        var script = await _client.GetTestScript(testCaseName);

        return new List<Step>
        {
            new()
            {
                Action = script.Text,
                Expected = string.Empty,
                TestData = string.Empty,
                Attachments = new List<string>()
            }
        };
    }
}
