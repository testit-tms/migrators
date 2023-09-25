using AzureExporter.Client;
using AzureExporter.Models;
using JsonWriter;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureExporter.Services;

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

    public async Task<List<string>> DownloadAttachments(List<AzureAttachment> attachments, Guid workItemId)
    {
        _logger.LogInformation("Downloading attachments");

        var names = new List<string>();

        foreach (var attachment in attachments)
        {
            _logger.LogDebug("Downloading attachment: {Name}", attachment.Name);

            var bytes = await _client.GetAttachmentById(attachment.Id);
            var name = await _writeService.WriteAttachment(workItemId, bytes, attachment.Name);
            names.Add(name);
        }

        _logger.LogDebug("Ending downloading attachments: {@Names}", names);

        return names;
    }
}
