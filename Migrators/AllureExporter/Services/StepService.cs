using System.Text;
using AllureExporter.Client;
using AllureExporter.Models;
using Microsoft.Extensions.Logging;
using Models;

namespace AllureExporter.Services;

public class StepService : IStepService
{
    private readonly ILogger<StepService> _logger;
    private readonly IClient _client;

    public StepService(ILogger<StepService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<List<Step>> ConvertSteps(int testCaseId)
    {
        var steps = await _client.GetSteps(testCaseId);

        _logger.LogDebug("Found steps: {@Steps}", steps);

        return steps.Select(allureStep =>
            {
                var attachments = new List<string>();

                foreach (var allureStepStep in allureStep.Steps.Where(allureStepStep =>
                             allureStepStep.Attachments != null))
                {
                    attachments.AddRange(allureStepStep.Attachments!.Select(a => a.Name));
                }

                var step = new Step
                {
                    Action = GetStepAction(allureStep),
                    Attachments = allureStep.Attachments != null
                        ? allureStep.Attachments.Select(a => a.Name).ToList()
                        : new List<string>(),
                    Expected = allureStep.ExpectedResult
                };

                step.Attachments.AddRange(attachments);

                return step;
            })
            .ToList();
    }

    private static string GetStepAction(AllureStep step)
    {
        var builder = new StringBuilder();

        if (!string.IsNullOrEmpty(step.Keyword))
        {
            builder.AppendLine($"<p>{step.Keyword}</p>");
        }

        builder.AppendLine($"<p>{step.Name}</p>");

        step.Steps
            .ForEach(s =>
            {
                if (!string.IsNullOrEmpty(s.Keyword))
                {
                    builder.AppendLine($"<p>{s.Keyword}</p>");
                }

                builder.AppendLine($"<p>{s.Name}</p>");
            });

        return builder.ToString();
    }
}
