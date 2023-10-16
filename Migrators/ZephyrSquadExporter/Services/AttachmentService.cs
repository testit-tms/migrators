using JsonWriter;
using Microsoft.Extensions.Logging;
using ZephyrSquadExporter.Client;

namespace ZephyrSquadExporter.Services;

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

    public async Task<List<string>> GetAttachmentsFromExecution(Guid testCaseId, string issueId, string entityId)
    {
        _logger.LogInformation("Getting attachments from execution {IssueId}", issueId);

        var listOfAttachments = new List<string>();

        var attachments = await _client.GetAttachmentsFromExecution(issueId, entityId);

        foreach (var attachment in attachments)
        {
            var attachmentBytes = await _client.GetAttachmentFromExecution(issueId, attachment.Id);

            var name = await _writeService.WriteAttachment(testCaseId, attachmentBytes, attachment.Name);

            listOfAttachments.Add(name);
        }

        _logger.LogDebug("Found {AttachmentCount} attachments: {Attachments}", listOfAttachments.Count,
            listOfAttachments);

        return listOfAttachments;
    }

    public async Task<string> GetAttachmentsFromStep(Guid testCaseId, string issueId, string attachmentId, string attachmentName)
    {
        _logger.LogInformation("Getting attachments from step {IssueId}", issueId);

        var attachmentBytes = await _client.GetAttachmentFromExecution(issueId, attachmentId);

        return await _writeService.WriteAttachment(testCaseId, attachmentBytes, attachmentName);
    }
}
