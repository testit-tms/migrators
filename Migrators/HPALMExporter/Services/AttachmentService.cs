using HPALMExporter.Client;
using HPALMExporter.Models;
using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;

namespace HPALMExporter.Services;

public class AttachmentService : IAttachmentService
{
    private readonly ILogger<AttachmentService> _logger;
    private readonly IClient _client;
    private readonly IWriteService _writeService;

    public AttachmentService(ILogger<AttachmentService> logger, IClient client, IWriteService writeService)
    {
        _logger = logger;
        _client = client;
        _writeService = writeService;
    }

    public async Task<AttachmentData> ConvertAttachmentsFromTest(Guid testCaseId, int testId)
    {
        _logger.LogInformation("Convert attachments from HP ALM for test {TestId}", testId);

        var attachments = await _client.GetAttachmentsFromTest(testId);

        var attachmentData = await ConvertAttachments(testCaseId, testId, attachments);

        return attachmentData;
    }

    public async Task<AttachmentData> ConvertAttachmentsFromStep(Guid testCaseId, int stepId)
    {
        _logger.LogInformation("Convert attachments from HP ALM for step {StepId}", stepId);

        var attachments = await _client.GetAttachmentsFromStep(stepId);

        var attachmentData = await ConvertAttachments(testCaseId, stepId, attachments);

        return attachmentData;
    }

    private async Task<AttachmentData> ConvertAttachments(Guid testCaseId, int entityId,
        IEnumerable<HPALMAttachment> attachments)
    {
        var attachmentData = new AttachmentData
        {
            Attachments = new List<string>(),
            Links = new List<Link>()
        };

        foreach (var attachment in attachments)
        {
            if (attachment.Type == HPALMAttachmentType.Url)
            {
                attachmentData.Links.Add(new Link
                {
                    Url = attachment.Description,
                    Title = attachment.Name
                });
            }
            else
            {
                var bytes = await _client.DownloadAttachment(entityId, attachment.Name);
                var name = await _writeService.WriteAttachment(testCaseId, bytes, attachment.Name);

                attachmentData.Attachments.Add(name);
            }
        }

        return attachmentData;
    }
}
