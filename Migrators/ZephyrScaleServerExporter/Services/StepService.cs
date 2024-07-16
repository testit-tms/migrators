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
    private readonly IClient _client;

    public StepService(ILogger<StepService> logger, IAttachmentService attachmentService, IClient client)
    {
        _logger = logger;
        _attachmentService = attachmentService;
        _client = client;
    }

    public async Task<List<Step>> ConvertSteps(Guid testCaseId, ZephyrTestScript testScript)
    {
        _logger.LogInformation("Converting steps from test script {@TestScript}", testScript);

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

            return stepList;
        }

        if (testScript.Text != null)
        {
            return new List<Step>
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
            };
        }

        return new List<Step>();
    }

    public async Task<List<Step>> ConvertSteps(Guid testCaseId, ZephyrArchivedTestScript testScript)
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
            Action = action.Description,
            Expected = expected.Description,
            TestData = testData.Description,
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

        try
        {
            var zephyrTestCase = await _client.GetTestCase(testCaseKey);

            return zephyrTestCase.TestScript != null ?
                await ConvertSteps(testCaseId, zephyrTestCase.TestScript) : new List<Step>();
        }
        catch (Exception ex)
        {
            var zephyrArchivedTestCase = await _client.GetArchivedTestCase(testCaseKey);

            return zephyrArchivedTestCase.TestScript != null ?
                await ConvertSteps(testCaseId, zephyrArchivedTestCase.TestScript) : new List<Step>();
        }
    }
}
