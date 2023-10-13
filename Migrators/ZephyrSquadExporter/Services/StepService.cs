using Microsoft.Extensions.Logging;
using Models;
using ZephyrSquadExporter.Client;

namespace ZephyrSquadExporter.Services;

public class StepService : IStepService
{
    private readonly ILogger<StepService> _logger;
    private readonly IClient _client;

    public StepService(ILogger<StepService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<List<Step>> ConvertSteps(string issueId)
    {
        _logger.LogInformation("Converting steps for issue {issueId}", issueId);

        var steps = await _client.GetSteps(issueId);

        return steps.Select(s =>
                new Step
                {
                    Action = s.Step,
                    Expected = s.Result,
                    TestData = s.Data,
                    ActionAttachments = new List<string>(),
                    ExpectedAttachments = new List<string>(),
                    TestDataAttachments = new List<string>()
                })
            .ToList();
    }
}
