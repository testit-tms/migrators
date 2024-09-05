using JsonWriter;
using Microsoft.Extensions.Logging;
using QaseExporter.Client;
using QaseExporter.Models;

namespace QaseExporter.Services;

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

    public async Task<List<string>> DownloadAttachments(List<QaseAttachment> qaseAttachments, Guid workItemId)
    {
        var names = new List<string>();

        foreach (var qaseAttachment in qaseAttachments)
        {
            _logger.LogDebug("Downloading attachment: {Name}", qaseAttachment.Name);

            try
            {
                var content = await _client.DownloadAttachment(qaseAttachment.Url);

                var name = await _writeService.WriteAttachment(workItemId, content, qaseAttachment.Name);

                names.Add(name);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to download attachment {@Attachment}. Error: {Ex}", qaseAttachment, ex);
            }
        }

        _logger.LogDebug("Ending downloading attachments: {@Names}", names);

        return names;
    }
}
