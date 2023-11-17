using PractiTestExporter.Models;
using JsonWriter;
using Microsoft.Extensions.Logging;
using PractiTestExporter.Client;

namespace PractiTestExporter.Services;

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

    public async Task<List<string>> DownloadAttachments(string entityType, string id, Guid workItemId)
    {
        _logger.LogInformation("Getting attachments by {EntityType} id {Id}", entityType, id);

        var names = new List<string>();

        var practiTestAttachments = await _client.GetAttachmentsByEntityId(entityType, id);

        foreach (var practiTestAttachment in practiTestAttachments)
        {
            _logger.LogInformation("Downloading attachment {Name} by id: {Id}", practiTestAttachment.Attributes.Name, practiTestAttachment.Id);

            var contentType = await _client.DownloadAttachmentById(practiTestAttachment.Id);

            var name = await _writeService.WriteAttachment(workItemId, contentType, practiTestAttachment.Attributes.Name);

            names.Add(name);
        }

        _logger.LogDebug("Ending downloading attachments: {@Names}", names);

        return names;
    }
}
