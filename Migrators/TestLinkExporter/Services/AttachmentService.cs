using TestLinkExporter.Models;
using JsonWriter;
using Microsoft.Extensions.Logging;
using TestLinkExporter.Client;

namespace TestLinkExporter.Services;

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

    public List<string> DownloadAttachments(int id, Guid workItemId)
    {
        _logger.LogInformation("Downloading attachments");

        var attachments = _client.GetAttachmentsByTestCaseId(id);

        var names = new List<string>();

        foreach (var attachment in attachments)
        {
            _logger.LogDebug("Downloading attachment: {Name}", attachment.Name);

            var name = _writeService.WriteAttachment(workItemId, attachment.Content, attachment.Name).Result;

            names.Add(name);
        }

        _logger.LogDebug("Ending downloading attachments: {@Names}", names);

        return names;
    }
}
