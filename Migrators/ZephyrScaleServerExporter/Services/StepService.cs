using Microsoft.Extensions.Logging;
using Models;
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
            var steps = testScript.Steps;

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

    private async Task<Step> ConvertStep(Guid testCaseId, ZephyrStep step)
    {
        var action = Utils.ExtractAttachments(step.Description);
        var expected = Utils.ExtractAttachments(step.ExpectedResult);
        var testData = Utils.ExtractAttachments(step.TestData);

        var newStep = new Step
        {
            Action = action.Description,
            Expected = expected.Description,
            TestData = testData.Description + $"<br><p>{step.CustomFields}</p>",
            ActionAttachments = new List<string>(),
            ExpectedAttachments = new List<string>(),
            TestDataAttachments = new List<string>()
        };

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

    private async Task<List<Step>> ConvertSharedSteps(Guid testCaseId, string testCaseKey)
    {
        _logger.LogInformation("Converting shared steps from test case key {testCaseKey}", testCaseKey);

        var zephyrTestCase = await _client.GetTestCase(testCaseKey);

        return zephyrTestCase.TestScript != null ?
            await ConvertSteps(testCaseId, zephyrTestCase.TestScript) : new List<Step>();
    }
}
