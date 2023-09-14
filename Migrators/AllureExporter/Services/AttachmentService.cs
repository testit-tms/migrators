using AllureExporter.Client;
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

    public async Task<List<string>> DownloadAttachments(int testCaseId, Guid id)
    {
        _logger.LogInformation("Downloading attachments");

        var attachments = await _client.GetAttachments(testCaseId);

        _logger.LogDebug("Found attachments: {@Attachments}", attachments);

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
