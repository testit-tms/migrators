using JsonWriter;
using Microsoft.Extensions.Logging;
using ZephyrScaleExporter.Models;

namespace ZephyrScaleExporter.Services;

public class AttachmentService : IAttachmentService
{
    private readonly ILogger<AttachmentService> _logger;
    private readonly IWriteService _writeService;
    private readonly HttpClient _httpClient;

    public AttachmentService(ILogger<AttachmentService> logger, IWriteService writeService)
    {
        _logger = logger;
        _writeService = writeService;
        _httpClient = new HttpClient();
    }

    public async Task<string> DownloadAttachment(Guid id, ZephyrAttachment attachment)
    {
        _logger.LogDebug("Downloading attachment {@Attachment}", attachment);

        var bytes = await _httpClient.GetByteArrayAsync(attachment.Url);

        return await _writeService.WriteAttachment(id, bytes, attachment.FileName);
    }
}
