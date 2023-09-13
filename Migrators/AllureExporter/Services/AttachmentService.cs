using AllureExporter.Client;
using AllureExporter.Models;
using JsonWriter;
using Microsoft.Extensions.Logging;

namespace AllureExporter.Services;

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

    public async Task<List<string>> DownloadAttachments(Guid id, IEnumerable<AllureAttachment> attachments)
    {
        _logger.LogInformation("Downloading attachments");

        var names = new List<string>();

        foreach (var attachment in attachments)
        {
            _logger.LogDebug("Downloading attachment: {Name}", attachment.Name);

            var bytes = await _client.DownloadAttachment(attachment.Id);
            var name = await _writeService.WriteAttachment(id, bytes, attachment.Name);
            names.Add(name);
        }

        _logger.LogDebug("Ending downloading attachments: {@Names}", names);

        return names;
    }
}
