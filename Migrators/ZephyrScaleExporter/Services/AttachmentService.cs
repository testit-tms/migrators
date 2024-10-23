using JsonWriter;
using Microsoft.Extensions.Logging;
using ZephyrScaleExporter.Client;
using ZephyrScaleExporter.Models;

namespace ZephyrScaleExporter.Services;

public class AttachmentService : IAttachmentService
{
    private readonly ILogger<AttachmentService> _logger;
    private readonly IWriteService _writeService;
    private readonly IClient _client;

    public AttachmentService(ILogger<AttachmentService> logger, IWriteService writeService, IClient client)
    {
        _logger = logger;
        _writeService = writeService;
        _client = client;
    }

    public async Task<string> DownloadAttachment(Guid id, ZephyrAttachment attachment)
    {
        _logger.LogDebug("Downloading attachment {@Attachment}", attachment);

        var bytes = await _client.DownloadAttachment(attachment.Url);

        return await _writeService.WriteAttachment(id, bytes, attachment.FileName);
    }

    public async Task<List<string>> DownloadAttachments(Guid id, List<ZephyrAttachment> attachments)
    {
        var names = new List<string>();

        foreach (var attachment in attachments)
        {
            _logger.LogDebug("Downloading attachment: {Name}", attachment.FileName);

            try
            {
                var bytes = await _client.DownloadAttachment(attachment.Url);

                var name = await _writeService.WriteAttachment(id, bytes, attachment.FileName);

                names.Add(name);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to download attachment {@Attachment}. Error: {Ex}", attachment, ex);
            }
        }

        _logger.LogDebug("Ending downloading attachments: {@Names}", names);

        return names;
    }
}
