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
        var commonAttachments = await _client.GetAttachments(testCaseId);

        _logger.LogDebug("Found steps: {@Steps}", steps);

        return steps.Select(allureStep =>
            {
                var attachments = new List<string>();

                foreach (var allureStepStep in allureStep.Steps.Where(allureStepStep =>
                             allureStepStep.Attachments != null))
                {
                    attachments.AddRange(GetAttachments(allureStepStep.Attachments!, commonAttachments));
                }

                var step = new Step
                {
                    Action = GetStepAction(allureStep),
                    ActionAttachments = allureStep.Attachments != null
                        ? allureStep.Attachments.Select(a => a.Name).ToList()
                        : new List<string>(),
                    ExpectedAttachments = new List<string>(),
                    TestDataAttachments = new List<string>(),
                    Expected = allureStep.ExpectedResult
                };

                step.ActionAttachments.AddRange(attachments);

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

    private static List<string> GetAttachments(
        List<AllureAttachment> stepAttachments,
        List<AllureAttachment> commonAttachments)
    {
        var attachments = new List<string>();

        foreach (var stepAttachment in stepAttachments)
        {
            var attachment = commonAttachments.FirstOrDefault(
                a => a.Id.Equals(stepAttachment.Id)
            );

            if (attachment == null)
            {
                continue;
            }

            attachments.Add(stepAttachment.Name);
        }

        return attachments;
    }
}
