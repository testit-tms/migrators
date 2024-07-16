using Microsoft.Extensions.Logging;
using Models;
using ZephyrScaleExporter.Client;

namespace ZephyrScaleExporter.Services;

public class StepService : IStepService
{
    private readonly ILogger<StepService> _logger;
    private readonly IClient _client;
    private readonly IAttachmentService _attachmentService;

    private const string TestSteps = "teststeps";

    public StepService(ILogger<StepService> logger, IClient client, IAttachmentService attachmentService)
    {
        _logger = logger;
        _client = client;
        _attachmentService = attachmentService;
    }

    public async Task<List<Step>> ConvertSteps(Guid testCaseId, string testCaseName, string testScript)
    {
        _logger.LogInformation("Converting steps for test case {TestCaseName}", testCaseName);

        if (testScript.Contains(TestSteps))
        {
            var steps = await _client.GetSteps(testCaseName);

            var stepList = new List<Step>();

            foreach (var step in steps)
            {
                if (step.Inline == null)
                {
                    continue;
                }

                var action = Utils.ExtractAttachments(step.Inline.Description);
                var expected = Utils.ExtractAttachments(step.Inline.ExpectedResult);
                var testData = Utils.ExtractAttachments(step.Inline.TestData);

                var newStep = new Step
                {
                    Action = action.Description,
                    Expected = expected.Description,
                    TestData = testData.Description + $"<br><p>{step.Inline.CustomFields}</p>",
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

                stepList.Add(newStep);
            }

            _logger.LogDebug("Steps: {@StepList}", stepList);

            return stepList;
        }

        var script = await _client.GetTestScript(testCaseName);

        return new List<Step>
        {
            new()
            {
                Action = script.Text,
                Expected = string.Empty,
                TestData = string.Empty,
                ActionAttachments = new List<string>(),
                ExpectedAttachments = new List<string>(),
                TestDataAttachments = new List<string>()
            }
        };
    }
}
