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

    public async Task<List<string>> DownloadAttachments(List<WorkItemRelation> relations, Guid workItemId)
    {
        _logger.LogInformation("Downloading attachments");

        var attachments = await ConvertAttachments(relations);

        var names = new List<string>();

        foreach (var attachment in attachments)
        {
            _logger.LogDebug("Downloading attachment: {Name}", attachment);

            var bytes = await _client.GetAttachmentById(attachment.Id);
            var name = await _writeService.WriteAttachment(workItemId, bytes, attachment.Name);
            names.Add(name);
        }

        _logger.LogDebug("Ending downloading attachments: {@Names}", names);

        return names;
    }

    private async Task<List<AzureAttachment>> ConvertAttachments(List<WorkItemRelation> relations)
    {
        var attachments = new List<AzureAttachment>();

        foreach (var relation in relations)
        {
            if (relation.Rel.Equals("AttachedFile"))
            {
                attachments.Add(
                    new AzureAttachment
                        {
                            Name = relation.Attributes["name"] as string,
                            Id = await GetGuidFromUrl(relation.Url)
                        }
                    );
            }
        }

        return attachments;
    }

    private async Task<Guid> GetGuidFromUrl(string url)
    {
        return new Guid(url.Substring(url.Length - 36));
    }
}
