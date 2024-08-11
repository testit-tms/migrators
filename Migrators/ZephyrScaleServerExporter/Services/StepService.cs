using Microsoft.Extensions.Logging;
using Models;
using System.Text.RegularExpressions;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models;

namespace ZephyrScaleServerExporter.Services;

public class StepService : IStepService
{
    private readonly ILogger<StepService> _logger;
    private readonly IAttachmentService _attachmentService;
    private readonly IParameterService _parameterService;
    private readonly IClient _client;
    private List<Iteration> _iterations;
    private const string START_STEP_PARAMETER = "<span class=\"atwho-inserted\" data-atwho-at-query=\"{\">{";
    private const string END_STEP_PARAMETER = "}</span>";
    private readonly Dictionary<string, List<Step>> _sharedStepsData = new Dictionary<string, List<Step>>();

    public StepService(ILogger<StepService> logger, IAttachmentService attachmentService,
        IParameterService parameterService, IClient client)
    {
        _logger = logger;
        _attachmentService = attachmentService;
        _parameterService = parameterService;
        _client = client;
    }

    public async Task<StepsData> ConvertSteps(Guid testCaseId, ZephyrTestScript testScript, List<Iteration> iterations)
    {
        _logger.LogInformation("Converting steps from test script {@TestScript}", testScript);

        _iterations = iterations;

        if (testScript.Steps != null)
        {
            var steps = testScript.Steps.OrderBy(s => s.Index).ToList();

            var stepList = new List<Step>();

            foreach (var step in steps)
            {
                if (string.IsNullOrEmpty(step.TestCaseKey))
                {
                    var newStep = await ConvertStep(testCaseId, step);

                    stepList.Add(newStep);
                }
                else
                {
                    var sharedSteps = await ConvertSharedSteps(testCaseId, step.TestCaseKey);

                    stepList.AddRange(sharedSteps);
                }
            }

            _logger.LogDebug("Steps: {@StepList}", stepList);

            return new StepsData
            {
                Steps = stepList,
                Iterations = _iterations
            };
        }

        if (testScript.Text != null)
        {
            return new StepsData
            {
                Steps = new List<Step>
                {
                    new()
                    {
                        Action = testScript.Text,
                        Expected = string.Empty,
                        TestData = string.Empty,
                        ActionAttachments = new List<string>(),
                        ExpectedAttachments = new List<string>(),
                        TestDataAttachments = new List<string>()
                    }
                },
                Iterations = _iterations
            };
        }

        return new StepsData
        {
            Steps = new List<Step>(),
            Iterations = _iterations
        };
    }

    private async Task<List<Step>> ConvertArchivedSteps(Guid testCaseId, ZephyrArchivedTestScript testScript)
    {
        _logger.LogInformation("Converting steps from test script {@TestScript}", testScript);

        if (testScript.StepScript != null && testScript.StepScript.Steps != null)
        {
            var steps = testScript.StepScript.Steps.OrderBy(s => s.Index).ToList();

            var stepList = new List<Step>();

            foreach (var step in steps)
            {
                if (string.IsNullOrEmpty(step.TestCaseKey))
                {
                    var newStep = await ConvertStep(testCaseId, step);

                    stepList.Add(newStep);
                }
                else
                {
                    var sharedSteps = await ConvertSharedSteps(testCaseId, step.TestCaseKey);

                    stepList.AddRange(sharedSteps);
                }
            }

            _logger.LogDebug("Steps: {@StepList}", stepList);

            return stepList;
        }

        if (testScript.TextScript != null && testScript.TextScript.Text != null)
        {
            return new List<Step>
            {
                new()
                {
                    Action = testScript.TextScript.Text,
                    Expected = string.Empty,
                    TestData = string.Empty,
                    ActionAttachments = new List<string>(),
                    ExpectedAttachments = new List<string>(),
                    TestDataAttachments = new List<string>()
                }
            };
        }

        return new List<Step>();
    }

    private async Task<Step> ConvertStep(Guid testCaseId, ZephyrStep step)
    {
        var action = Utils.ExtractAttachments(step.Description);
        var expected = Utils.ExtractAttachments(step.ExpectedResult);
        var testData = Utils.ExtractAttachments(step.TestData);

        var newStep = new Step
        {
            Action = AddParametersToStep(action.Description),
            Expected = AddParametersToStep(expected.Description),
            TestData = AddParametersToStep(testData.Description),
            ActionAttachments = new List<string>(),
            ExpectedAttachments = new List<string>(),
            TestDataAttachments = new List<string>()
        };

        if (step.CustomFields != null)
        {
            foreach (var customField in step.CustomFields)
            {

                newStep.TestData += ConvertCustomField(customField);
            }
        }

        if (action.Attachments.Count > 0)
        {
            foreach (var attachment in action.Attachments)
            {
                var fileName = await _attachmentService.DownloadAttachment(testCaseId, attachment);
                newStep.ActionAttachments.Add(fileName);
            }
        }

        if (expected.Attachments.Count > 0)
        {
            foreach (var attachment in expected.Attachments)
            {
                var fileName = await _attachmentService.DownloadAttachment(testCaseId, attachment);
                newStep.ExpectedAttachments.Add(fileName);
            }
        }

        if (testData.Attachments.Count > 0)
        {
            foreach (var attachment in testData.Attachments)
            {
                var fileName = await _attachmentService.DownloadAttachment(testCaseId, attachment);
                newStep.TestDataAttachments.Add(fileName);
            }
        }

        return newStep;
    }

    private string ConvertCustomField(ZephyrCustomField customField)
    {
        _logger.LogInformation("Converting custom field \"{Name}\" for step", customField.CustomField.Name);

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

        _logger.LogInformation("Failed to convert empty custom field \"{Name}\" for step", customField.CustomField.Name);

        return "";
    }

    private string ConvertOptionsFromCustomField(ZephyrCustomField customField)
    {
        _logger.LogInformation("Converting custom field with options \"{Name}\" for step", customField.CustomField.Name);

        if (customField.IntValue != null && customField.CustomField.Options != null)
        {
            return $"<br><p>{customField.CustomField.Name}: {customField.CustomField.Options.Find(o => o.Id == customField.IntValue.GetValueOrDefault()).Name}</p>";
        }

        if (customField.StringValue != null && customField.CustomField.Options != null)
        {
            var ids = ConvertStringToIds(customField.StringValue);
            var options = "";

            foreach (var id in ids)
            {
                options += customField.CustomField.Options.Find(o => o.Id == id).Name + ", ";
            }

            return $"<br><p>{customField.CustomField.Name}: {options}</p>";
        }

        _logger.LogInformation("Failed to convert empty custom field with options \"{Name}\" for step", customField.CustomField.Name);

        return "";
    }

    private List<int> ConvertStringToIds(string strWithIds)
    {

        var ids = new List<int>();
        string pattern = @"\d+";
        Regex reg = new Regex(pattern);
        MatchCollection m = reg.Matches(strWithIds);

        for (int i = 0; i < m.Count; i++)
        {
            ids.Add(int.Parse(m[i].Value));
        }

        return ids;
    }

    private async Task<List<Step>> ConvertSharedSteps(Guid testCaseId, string testCaseKey)
    {
        _logger.LogInformation("Converting shared steps from test case key {testCaseKey}", testCaseKey);

        if (_sharedStepsData.ContainsKey(testCaseKey))
        {
            _logger.LogInformation("Return shared steps {@Steps}", _sharedStepsData[testCaseKey]);

            return _sharedStepsData[testCaseKey];
        }

        var sharedStepsIterations = await _parameterService.ConvertParameters(testCaseKey);
        _iterations = _parameterService.MergeIterations(_iterations, sharedStepsIterations);

        try
        {
            var zephyrTestCase = await _client.GetTestCase(testCaseKey);

            var sharedSteps = zephyrTestCase.TestScript != null ?
                (await ConvertSteps(testCaseId, zephyrTestCase.TestScript, _iterations)).Steps : new List<Step>();

            _sharedStepsData.Add(testCaseKey, sharedSteps);

            return sharedSteps;
        }
        catch (Exception ex)
        {
            var zephyrArchivedTestCase = await _client.GetArchivedTestCase(testCaseKey);

            var archivedSharedSteps = zephyrArchivedTestCase.TestScript != null ?
                await ConvertArchivedSteps(testCaseId, zephyrArchivedTestCase.TestScript) : new List<Step>();

            _sharedStepsData.Add(testCaseKey, archivedSharedSteps);

            return archivedSharedSteps;
        }
    }

    private string AddParametersToStep(string stepText)
    {
        if (stepText.Contains(START_STEP_PARAMETER) && stepText.Contains(END_STEP_PARAMETER))
        {
            foreach (var iteration in _iterations)
            {
                foreach (var parameter in iteration.Parameters)
                {
                    stepText = stepText.Replace(START_STEP_PARAMETER + parameter.Name + END_STEP_PARAMETER, $"<<<{parameter.Name}>>>");
                }
            }
        }

        return stepText;
    }
}
