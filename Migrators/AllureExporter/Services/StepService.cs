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

    public async Task<List<Step>> ConvertStepsForTestCase(int testCaseId, Dictionary<string, Guid> sharedStepMap)
    {
        var steps = await _client.GetSteps(testCaseId);
        var commonAttachments = await _client.GetAttachmentsByTestCaseId(testCaseId);

        _logger.LogDebug("Found steps: {@Steps}", steps);

        if (steps.Any())
        {
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

        var stepsInfo = await _client.GetStepsInfoByTestCaseId(testCaseId);

        _logger.LogDebug("Found stepsInfo by test case id {TestCaseId}: {@StepsInfo}", testCaseId, stepsInfo);

        return ConvertStepsFromStepsInfo(stepsInfo.Root.NestedStepIds, stepsInfo, commonAttachments, sharedStepMap);
    }

    private static List<Step> ConvertStepsFromStepsInfo(
        List<int> nestedStepIds,
        AllureStepsInfo stepsInfo,
        List<AllureAttachment> commonAttachments,
        Dictionary<string, Guid> sharedStepMap)
    {
        var steps = new List<Step>();

        foreach (int stepId in nestedStepIds)
        {
            var allureStep = stepsInfo.ScenarioStepsDictionary[stepId.ToString()];

            var step = new Step
            {
                Action = allureStep.Body != null ? allureStep.Body : string.Empty,
                Expected = allureStep.ExpectedResult != null ? allureStep.ExpectedResult : string.Empty,
                ActionAttachments = new List<string>(),
                ExpectedAttachments = new List<string>(),
                TestDataAttachments = new List<string>(),
            };

            if (allureStep.SharedStepId != null)
            {
                step.SharedStepId = sharedStepMap[allureStep.SharedStepId.ToString()];
            }

            if (allureStep.AttachmentId != null)
            {
                step.ActionAttachments.AddRange(
                    GetAttachments(
                        [stepsInfo.AttachmentsDictionary[allureStep.AttachmentId.ToString()]],
                        commonAttachments));
            }

            steps.Add(step);

            if (allureStep.NestedStepIds != null)
            {
                var nestedSteps = ConvertStepsFromStepsInfo(allureStep.NestedStepIds, stepsInfo, commonAttachments, sharedStepMap);

                steps.AddRange(nestedSteps);
            }
        }

        return steps;
    }

    public async Task<List<Step>> ConvertStepsForSharedStep(int sharedStepId)
    {
        var stepsInfo = await _client.GetStepsInfoBySharedStepId(sharedStepId);
        var commonAttachments = await _client.GetAttachmentsBySharedStepId(sharedStepId);

        _logger.LogDebug("Found stepsInfo by shared step id {SharedStepId}: {@StepsInfo}", sharedStepId, stepsInfo);

        return ConvertStepsFromSharedStepsInfo(stepsInfo.Root.NestedStepIds, stepsInfo, commonAttachments);
    }

    private static List<Step> ConvertStepsFromSharedStepsInfo(
    List<int> nestedStepIds,
    AllureSharedStepsInfo stepsInfo,
    List<AllureAttachment> commonAttachments)
    {
        var steps = new List<Step>();

        foreach (int stepId in nestedStepIds)
        {
            var allureStep = stepsInfo.SharedStepScenarioStepsDictionary[stepId.ToString()];

            if (allureStep.SharedStepId != null)
            {
                continue;
            }

            var step = new Step
            {
                Action = allureStep.Body != null ? allureStep.Body : string.Empty,
                Expected = allureStep.ExpectedResult != null ? allureStep.ExpectedResult : string.Empty,
                ActionAttachments = new List<string>(),
                ExpectedAttachments = new List<string>(),
                TestDataAttachments = new List<string>(),
            };

            if (allureStep.AttachmentId != null)
            {
                step.ActionAttachments.AddRange(
                    GetAttachments(
                        [stepsInfo.SharedStepAttachmentsDictionary[allureStep.AttachmentId.ToString()]],
                        commonAttachments));
            }

            steps.Add(step);

            if (allureStep.NestedStepIds != null)
            {
                var nestedSteps = ConvertStepsFromSharedStepsInfo(allureStep.NestedStepIds, stepsInfo, commonAttachments);

                steps.AddRange(nestedSteps);
            }
        }

        return steps;
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
