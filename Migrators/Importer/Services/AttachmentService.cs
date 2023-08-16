using Importer.Client;
using Microsoft.Extensions.Logging;

namespace Importer.Services;

public class AttachmentService : IAttachmentService
{
    private readonly ILogger<AttachmentService> _logger;
    private readonly IClient _client;
    private readonly IParserService _parserService;

    public AttachmentService(ILogger<AttachmentService> logger, IClient client, IParserService parserService)
    {
        _logger = logger;
        _client = client;
        _parserService = parserService;
    }

    public async Task<Dictionary<string, Guid>> GetAttachments(Guid workItemId, IEnumerable<string> attachments)
    {
        _logger.LogInformation("Importing attachments for work item {Id}", workItemId);

        Dictionary<string, Guid> ids = new();

        foreach (var attachment in attachments)
        {
            var stream = await _parserService.GetAttachment(workItemId, attachment);
            var id = await _client.UploadAttachment(attachment, stream);

            ids.Add(attachment, id);
        }

        return ids;
    }
}
