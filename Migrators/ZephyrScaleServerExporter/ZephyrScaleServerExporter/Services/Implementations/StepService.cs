using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Models;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models.Attachment;
using ZephyrScaleServerExporter.Models.TestCases;
using ZephyrScaleServerExporter.Services.Helpers;

namespace ZephyrScaleServerExporter.Services.Implementations;

internal partial class StepService(
    IDetailedLogService detailedLogService,
    IAttachmentService attachmentService,
    ILogger<StepService> logger,
    IParameterService parameterService,
    IClient client)
    : IStepService
{
    private const string StartStepParameter = "<span class=\"atwho-inserted\" data-atwho-at-query=\"{\">{";
    private const string EndStepParameter = "}</span>";
    private readonly Dictionary<string, List<Step>> _sharedStepsData = new();
    private const string StringIdPattern = @"\d+";

    /// <summary>
    /// Download `currentStep.TestCaseKey`, listed in `sharedSteps` for `testCaseId`
    /// </summary>
    /// <returns>List of downloaded files</returns>
    private async Task<List<ZephyrAttachment>> DownloadSharedStepsAttachments(Guid testCaseId,
        ZephyrStep currentStep, List<Step> sharedSteps)
    {
        try
        {
            if (currentStep.TestCaseKey == null)
                return [];

            var targetAttachments = new List<string>();
            sharedSteps.ForEach(s => { targetAttachments.AddRange(s.GetAllAttachments()); });
            var allCaseAttachments = await client.GetAttachmentsForTestCase(currentStep.TestCaseKey);

            var sharedAttachments = allCaseAttachments
                .Where(x => targetAttachments.Contains(x.FileName)).ToList();

            var tasks = sharedAttachments.AsParallel()
                .WithDegreeOfParallelism(Utils.GetLogicalProcessors()).Select(async x =>
                {
                    x.FileName = await attachmentService.DownloadAttachment(testCaseId, x, true);
                    return x;
                }).ToList();
            var results = await Task.WhenAll(tasks);
            sharedAttachments = results.ToList();

            return sharedAttachments;
        }
        catch (Exception e)
        {
            logger.LogWarning("Failed to get shared step attachment: {Message}, trace: {StackTrace}",
                e.Message, e.StackTrace);
        }
        return [];
    }

    private async Task<List<string>> HandleStepAttachments(Guid testCaseId, ZephyrStep step)
    {
        List<string> attachments = new List<string>();
        if (step.Attachments == null || step.Attachments.Count == 0)
        {
            return attachments;
        }
        var calls = step.Attachments
            .AsParallel()
            .WithDegreeOfParallelism(Utils.GetLogicalProcessors())
            .Select(async x => await attachmentService
                .DownloadAttachmentById(testCaseId, x!, true)).ToList();
        var fileNames = await Task.WhenAll(calls);
        attachments.AddRange(fileNames);

        return attachments;
    }

    // 2.2 Общий шаг MTIX-T140
    private void AddSharedPreString(List<Step> sharedSteps, int sectionNumber, string testCaseKey)
    {
        var j = 0;
        foreach (var sharedStep in sharedSteps)
        {
            j++;
            var preString = $"{sectionNumber}.{j} Общий шаг {testCaseKey}\n";
            detailedLogService.LogInformation(preString);
            sharedStep.Action = preString + sharedStep.Action;
        }
    }

    private static StepsData HandleTextTestScript(ZephyrTestScript testScript, List<Iteration> iterations)
    {
        return new StepsData
        {
            Steps =
            [
                new()
                {
                    Action = Utils.ConvertingFormatCharacters(testScript.Text),
                    Expected = string.Empty,
                    TestData = string.Empty,
                    ActionAttachments = new List<string>(),
                    ExpectedAttachments = new List<string>(),
                    TestDataAttachments = new List<string>()
                }
            ],
            Iterations = iterations
        };
    }

    private async Task<List<Step>> ProcessStepOrSharedStep(Guid testCaseId,
        ZephyrStep step, List<Iteration> iterations, int stepNumber)
    {
        if (string.IsNullOrEmpty(step.TestCaseKey))
        {
            var newStep = await ConvertStep(testCaseId, step, iterations);
            detailedLogService.LogInformation("Converted step: {@Step}", newStep);
            return [newStep];
        }

        // explicit copy before [AddSharedPreString] for duplicate prevention
        var sharedSteps = (await ConvertSharedSteps(testCaseId, step.TestCaseKey, iterations))
            .Select(Step.CopyFrom).ToList();
        detailedLogService.LogInformation("Converted shared steps: {@SharedSteps}", sharedSteps);
        var sharedAttachments = await
            DownloadSharedStepsAttachments(testCaseId, step, sharedSteps);
        detailedLogService.LogInformation("Converted shared attachments: {@SharedAttachments}", sharedAttachments);
        AddSharedPreString(sharedSteps, stepNumber, step.TestCaseKey);

        return sharedSteps;
    }

    private async Task<StepsData> HandleStepTestScript(Guid testCaseId, ZephyrTestScript testScript,
        List<Iteration> iterations)
    {
        if (testScript.Steps == null)
        {
            return new StepsData { Steps = [], Iterations = iterations };
        }
        var steps = testScript.Steps!.OrderBy(s => s.Index).ToList();

        var callList = new List<Task<List<Step>>>();
        var stepNumber = 0;
        foreach (var step in steps)
        {
            stepNumber++;
            callList.Add(ProcessStepOrSharedStep(testCaseId, step, iterations, stepNumber));
        }
        var results = await Task.WhenAll(callList.ToArray());

        var stepList = new List<Step>();
        results.ToList().ForEach(x => stepList.AddRange(x));

        return new StepsData
        {
            Steps = stepList,
            Iterations = iterations
        };
    }

    public async Task<StepsData> ConvertSteps(Guid testCaseId, ZephyrTestScript testScript, List<Iteration> iterations)
    {
        detailedLogService.LogInformation("Converting steps from test script {@TestScript}", testScript);

        if (testScript.Steps != null)
        {
            return await HandleStepTestScript(testCaseId, testScript, iterations);
        }
        if (testScript.Text != null)
        {
            return HandleTextTestScript(testScript, iterations);
        }

        return new StepsData
        {
            Steps = new List<Step>(),
            Iterations = iterations
        };
    }

    private async Task<List<Step>> ConvertArchivedSteps(Guid testCaseId,
        ZephyrArchivedTestScript testScript, List<Iteration> iterations)
    {
        detailedLogService.LogInformation("Converting steps from test script {@TestScript}", testScript);

        if (testScript.StepScript is { Steps: not null })
        {
            var steps = testScript.StepScript.Steps.OrderBy(s => s.Index).ToList();

            var stepList = new List<Step>();

            foreach (var step in steps)
            {
                if (string.IsNullOrEmpty(step.TestCaseKey))
                {
                    var newStep = await ConvertStep(testCaseId, step, iterations);

                    stepList.Add(newStep);
                }
                else
                {
                    var sharedSteps = await ConvertSharedSteps(testCaseId, step.TestCaseKey, iterations);

                    stepList.AddRange(sharedSteps);
                }
            }
            return stepList;
        }

        if (testScript.TextScript is { Text: not null })
        {
            return
            [
                new()
                {
                    Action = Utils.ConvertingFormatCharacters(testScript.TextScript.Text),
                    Expected = string.Empty,
                    TestData = string.Empty,
                    ActionAttachments = new List<string>(),
                    ExpectedAttachments = new List<string>(),
                    TestDataAttachments = new List<string>()
                }
            ];
        }

        return new List<Step>();
    }

    /// <summary>
    /// Download attachments files and place filenames to [to] list. Return false if there are no data downloaded.
    /// </summary>
    private async Task<bool> DownloadAttachments(Guid testCaseId, List<ZephyrAttachment> source, List<string> target)
    {
        if (source.Count == 0)
        {
            return false;
        }
        foreach (var attachment in source)
        {
            var fileName = await attachmentService.DownloadAttachment(testCaseId, attachment, true);
            Utils.AddIfUnique(target, fileName);
        }
        return true;
    }

    private async Task<Step> ConvertStep(Guid testCaseId, ZephyrStep step, List<Iteration> iterations)
    {
        // handle duplicate -> transform duplicate (on merge with regular attachments)
        var action = Utils.ExtractAttachments(step.Description);
        var expected = Utils.ExtractAttachments(step.ExpectedResult);
        var testData = Utils.ExtractAttachments(step.TestData);
        // handle step -> name transform
        var attachments = await HandleStepAttachments(testCaseId, step);

        var newStep = new Step
        {
            Action = Utils.ConvertingFormatCharacters(AddParametersToStep(action.Description, iterations)),
            Expected = AddParametersToStep(expected.Description, iterations),
            TestData = AddParametersToStep(testData.Description, iterations),
            ActionAttachments = new List<string>(),
            ExpectedAttachments = new List<string>(),
            TestDataAttachments = new List<string>(),
            Attachments = attachments
        };

        if (step.CustomFields != null)
        {
            foreach (var customField in step.CustomFields)
            {

                newStep.TestData += ConvertCustomField(customField);
            }
        }
        var t1 = DownloadAttachments(testCaseId, action.Attachments, newStep.ActionAttachments);
        var t2 = DownloadAttachments(testCaseId, expected.Attachments, newStep.ExpectedAttachments);
        var t3 = DownloadAttachments(testCaseId, testData.Attachments, newStep.TestDataAttachments);
        await Task.WhenAll(t1, t2, t3);

        return newStep;
    }

    private string ConvertCustomField(ZephyrCustomField customField)
    {
        detailedLogService.LogInformation("Converting custom field \"{Name}\" for step", customField.CustomField.Name);

        if (customField.CustomField.Options != null)
        {
            return ConvertOptionsFromCustomField(customField);
        }

        if (customField.IntValue != null)
        {
            return $"<br><p>{customField.CustomField.Name}: {customField.IntValue}</p>";
        }

        if (customField.StringValue != null)
        {
            return $"<br><p>{customField.CustomField.Name}: {customField.StringValue}</p>";
        }

        detailedLogService.LogInformation("Failed to convert empty custom field \"{Name}\" for step", customField.CustomField.Name);

        return "";
    }

    private string ConvertOptionsFromCustomField(ZephyrCustomField customField)
    {
        detailedLogService.LogInformation("Converting custom field with options \"{Name}\" for step", customField.CustomField.Name);

        if (customField is { IntValue: not null, CustomField.Options: not null })
        {
            return $"<br><p>{customField.CustomField.Name}: " +
                   $"{customField.CustomField.Options
                       .Find(o =>
                           o.Id == customField.IntValue.GetValueOrDefault())?.Name}</p>";
        }

        if (customField is { StringValue: not null, CustomField.Options: not null })
        {
            var ids = ConvertStringToIds(customField.StringValue);
            var optionsBuilder = new StringBuilder();

            foreach (var id in ids)
            {
                var optName = customField.CustomField.Options
                    .Find(o => o.Id == id)?.Name;
                optionsBuilder.Append(optName + ", ");
            }

            return $"<br><p>{customField.CustomField.Name}: {optionsBuilder}</p>";
        }

        detailedLogService.LogInformation("Failed to convert empty custom field with options \"{Name}\" for step", customField.CustomField.Name);

        return "";
    }

    private static List<int> ConvertStringToIds(string strWithIds)
    {

        var ids = new List<int>();

        var reg = StringIdRegex();
        MatchCollection m = reg.Matches(strWithIds);

        for (int i = 0; i < m.Count; i++)
        {
            ids.Add(int.Parse(m[i].Value));
        }

        return ids;
    }

    /// <summary>
    /// if testCaseKey already used somewhere and imported as shared steps container, then it
    /// cached in _sharedStepsData and the data can be used again.
    ///
    /// Proceed "AttachmentService.CopySharedAttachments" to copy attachment files to the testCaseId's folder.
    /// </summary>
    private async Task<List<Step>> UseSharedStepsDataForKey(Guid testCaseId, string testCaseKey)
    {
        var data = _sharedStepsData[testCaseKey];
        detailedLogService.LogInformation("Return saved shared steps {@Steps}", data);
        var taskList = data.AsParallel().WithDegreeOfParallelism(Utils.GetLogicalProcessors())
            .Select(async x => await attachmentService.CopySharedAttachments(testCaseId, x)).ToList();
        var res = await Task.WhenAll(taskList);
        detailedLogService.LogInformation("Copied steps attachments to the current testCase: {List} ", res.ToList());
        return data;
    }

    private async Task<List<Step>> ConvertSharedSteps(Guid testCaseId, string testCaseKey, List<Iteration> iterations)
    {
        detailedLogService.LogInformation("Converting shared steps from test case key {TestCaseKey}", testCaseKey);

        if (_sharedStepsData.ContainsKey(testCaseKey))
        {
            return await UseSharedStepsDataForKey(testCaseId, testCaseKey);
        }

        var sharedStepsIterations = await parameterService.ConvertParameters(testCaseKey);
        iterations = parameterService.MergeIterations(iterations, sharedStepsIterations);

        try
        {
            var zephyrTestCase = await client.GetTestCase(testCaseKey);
            if (zephyrTestCase.TestScript == null)
            {
                _sharedStepsData.TryAdd(testCaseKey, new List<Step>());
                return new List<Step>();
            }
            var sharedSteps = (await ConvertSteps(testCaseId, zephyrTestCase.TestScript, iterations)).Steps;
            _sharedStepsData.TryAdd(testCaseKey, sharedSteps);
            return sharedSteps;
        }
        catch (Exception)
        {
            var zephyrArchivedTestCase = await client.GetArchivedTestCase(testCaseKey);
            if (zephyrArchivedTestCase.TestScript == null)
            {
                _sharedStepsData.TryAdd(testCaseKey, new List<Step>());
                return new List<Step>();
            }
            var archivedSharedSteps = await ConvertArchivedSteps(testCaseId,
                zephyrArchivedTestCase.TestScript, iterations);
            _sharedStepsData.TryAdd(testCaseKey, archivedSharedSteps);

            return archivedSharedSteps;
        }
    }

    private static string AddParametersToStep(string stepText, List<Iteration> iterations)
    {
        if (stepText.Contains(StartStepParameter) && stepText.Contains(EndStepParameter))
        {
            foreach (var iteration in iterations)
            {
                foreach (var parameter in iteration.Parameters)
                {
                    stepText = stepText.Replace(StartStepParameter + parameter.Name + EndStepParameter, $"<<<{parameter.Name}>>>");
                }
            }
        }

        return stepText;
    }

    [GeneratedRegex(StringIdPattern)]
    private static partial Regex StringIdRegex();
}
