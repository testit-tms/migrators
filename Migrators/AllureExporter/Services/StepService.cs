using System.Text;
using AllureExporter.Client;
using AllureExporter.Models;
using Microsoft.Extensions.Logging;
using Models;
using Serilog;

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

    public async Task<List<Step>> ConvertStepsForTestCase(long testCaseId, Dictionary<string, Guid> sharedStepMap)
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

        return ConvertStepsFromStepsInfo(stepsInfo.Root!.NestedStepIds, stepsInfo, commonAttachments, sharedStepMap);
    }

    /// <summary>
    /// fills allureStep with expectedBody and return steps attachments
    /// </summary>
    private Dictionary<string, List<long>> FillExpectedResult(Dictionary<string, AllureScenarioStep> stepsDictionary)
    {
        Dictionary<string, List<long>> expectedAttachments = new();
        var stepsList = stepsDictionary.Values.ToList();
        stepsList.Sort((x, y) => x.Id.CompareTo(y.Id));

        stepsList
            .AsParallel()
            .Where(step => step.Body == "Expected Result")
            .ForAll(step =>
            {
                try
                {
                    var allureStep = stepsDictionary[(step.Id - 1).ToString()];

                    var expectedRes = "";
                    var expectedAttachmentIds = new List<long>();
                    foreach (var expectedId in step.NestedStepIds!)
                    {
                        AllureScenarioStep expectedStep = stepsDictionary[expectedId.ToString()];
                        if (expectedStep.Body != null) expectedRes += expectedStep.Body + ";";
                        if (expectedStep.AttachmentId != null)
                            expectedAttachmentIds.Add(expectedStep.AttachmentId.Value);
                    }

                    if (expectedRes.Length > 0)
                    {
                        expectedRes = expectedRes.Substring(0, expectedRes.Length - 1);
                    }

                    allureStep.ExpectedResult = expectedRes;
                    expectedAttachments.Add(allureStep.Id.ToString(), expectedAttachmentIds);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e.ToString());
                }
            });
        return expectedAttachments;
    }

    private void FillExpectedAttachments(
        Step step,
        List<long>? expectedAttachmentIds,
        Dictionary<string,AllureAttachment> attachmentsDirectory,
        List<AllureAttachment> commonAttachments)
    {
        if (expectedAttachmentIds != null)
        {
            var expAtts = expectedAttachmentIds.Select(x =>
                attachmentsDirectory[x.ToString()]).ToList();
            step.ExpectedAttachments.AddRange(
                GetAttachments(expAtts, commonAttachments));
        }
    }


    private List<Step> ConvertStepsFromStepsInfo(
        List<long> nestedStepIds,
        AllureStepsInfo stepsInfo,
        List<AllureAttachment> commonAttachments,
        Dictionary<string, Guid> sharedStepMap)
    {
        var steps = new List<Step>();
        var expectedAttachments = FillExpectedResult(stepsInfo.ScenarioStepsDictionary);

        foreach (long stepId in nestedStepIds)
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

            expectedAttachments.TryGetValue(allureStep.Id.ToString(),
                out var expectedAttachmentIds);
            FillExpectedAttachments(step, expectedAttachmentIds,
                stepsInfo.AttachmentsDictionary, commonAttachments);


            if (allureStep.SharedStepId != null)
            {
                step.SharedStepId = sharedStepMap[allureStep.SharedStepId.ToString()!];
            }

            if (allureStep.AttachmentId != null)
            {
                step.ActionAttachments.AddRange(
                    GetAttachments(
                        [stepsInfo.AttachmentsDictionary[allureStep.AttachmentId.ToString()!]],
                        commonAttachments));
            }

            steps.Add(step);

            if (allureStep.NestedStepIds != null)
            {
                var nestedSteps = ConvertStepsFromStepsInfo(
                    allureStep.NestedStepIds, stepsInfo, commonAttachments, sharedStepMap);

                steps.AddRange(nestedSteps);
            }
        }

        return steps;
    }

    public async Task<List<Step>> ConvertStepsForSharedStep(long sharedStepId)
    {
        var stepsInfo = await _client.GetStepsInfoBySharedStepId(sharedStepId);
        var commonAttachments = await _client.GetAttachmentsBySharedStepId(sharedStepId);

        _logger.LogDebug("Found stepsInfo by shared step id {SharedStepId}: {@StepsInfo}", sharedStepId, stepsInfo);

        return ConvertStepsFromSharedStepsInfo(stepsInfo.Root!.NestedStepIds, stepsInfo, commonAttachments);
    }

    private List<Step> ConvertStepsFromSharedStepsInfo(
    List<long> nestedStepIds,
    AllureSharedStepsInfo stepsInfo,
    List<AllureAttachment> commonAttachments)
    {
        var steps = new List<Step>();
        var expectedAttachments = FillExpectedResult(stepsInfo.SharedStepScenarioStepsDictionary);

        foreach (long stepId in nestedStepIds)
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

            expectedAttachments.TryGetValue(allureStep.Id.ToString(),
                out var expectedAttachmentIds);
            FillExpectedAttachments(step, expectedAttachmentIds,
                stepsInfo.SharedStepAttachmentsDictionary, commonAttachments);

            if (allureStep.AttachmentId != null)
            {
                step.ActionAttachments.AddRange(
                    GetAttachments(
                        [stepsInfo.SharedStepAttachmentsDictionary[allureStep.AttachmentId.ToString()!]],
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
