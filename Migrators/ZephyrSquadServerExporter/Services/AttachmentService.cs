using JsonWriter;
using Microsoft.Extensions.Logging;
using ZephyrSquadServerExporter.Client;
using ZephyrSquadServerExporter.Models;

namespace ZephyrSquadServerExporter.Services;

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

    public async Task<List<string>> GetAttachmentsForIssue(Guid testCaseId, List<IssueAttachment> attachments)
    {
        _logger.LogInformation("Getting attachments for issue");

        var listOfAttachments = new List<string>();

        foreach (var attachment in attachments)
        {
            var attachmentBytes = await _client.GetAttachmentForIssueById(attachment.Id, attachment.Name);

            var name = await _writeService.WriteAttachment(testCaseId, attachmentBytes, attachment.Name);

            listOfAttachments.Add(name);
        }

        _logger.LogDebug("Found {AttachmentCount} attachments: {Attachments}", listOfAttachments.Count,
            listOfAttachments);

        return listOfAttachments;
    }

    public async Task<string> GetAttachmentsForStep(Guid testCaseId, string issueId, string attachmentId, string attachmentName)
    {
        _logger.LogInformation("Getting attachments from step {IssueId}", issueId);

        var attachmentBytes = await _client.GetAttachmentForStepById(attachmentId);

        return await _writeService.WriteAttachment(testCaseId, attachmentBytes, attachmentName);
    }
}
