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

    public async Task<List<string>> DownloadAttachments(int id, Guid workItemId)
    {
        _logger.LogInformation("Getting attachments by test case id: {Id}", id);

        var attachments = _client.GetAttachmentsByTestCaseId(id);

        var names = new List<string>();

        foreach (var attachment in attachments)
        {
            _logger.LogDebug("Downloading attachment: {Name}", attachment.Name);

            var name = await _writeService.WriteAttachment(workItemId, attachment.Content, attachment.Name);

            names.Add(name);
        }

        _logger.LogDebug("Ending downloading attachments: {@Names}", names);

        return names;
    }
}
