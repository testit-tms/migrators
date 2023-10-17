using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;
using ZephyrSquadExporter.Client;

namespace ZephyrSquadExporter.Services;

public class StepService : IStepService
{
    private readonly ILogger<StepService> _logger;
    private readonly IClient _client;
    private readonly IAttachmentService _attachmentService;

    public StepService(ILogger<StepService> logger, IClient client, IAttachmentService attachmentService)
    {
        _logger = logger;
        _client = client;
        _attachmentService = attachmentService;
    }

    public async Task<List<Step>> ConvertSteps(Guid testCaseId, string issueId)
    {
        _logger.LogInformation("Converting steps for issue {issueId}", issueId);

        var listOfSteps = new List<Step>();
        var steps = await _client.GetSteps(issueId);

        foreach (var step in steps)
        {
            var attachments = new List<string>();
            var testData = string.Empty;

            foreach (var attachment in step.Attachments)
            {
                var attachmentName = await _attachmentService.GetAttachmentsFromStep(testCaseId, issueId,
                    attachment.Id, attachment.Name);
                testData += $"<p><<<{attachmentName}>>></p>";
                attachments.Add(attachmentName);
            }

            listOfSteps.Add(new Step
            {
                Action = step.Step,
                Expected = step.Result,
                TestData = step.Data + $"<p>{testData}</p>",
                ActionAttachments = new List<string>(),
                ExpectedAttachments = new List<string>(),
                TestDataAttachments = attachments
            });
        }

        return listOfSteps;
    }
}
