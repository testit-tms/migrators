using AzureExporter.Client;
using AzureExporter.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using Models;

namespace AzureExporter.Services;

public class AttachmentService : IAttachmentService
{
    private readonly ILogger<AttachmentService> _logger;
    private readonly IClient _client;

    public AttachmentService(ILogger<AttachmentService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<List<string>> DownloadAttachments(List<int> attachments)
    {
        _logger.LogInformation("Downloading attachments");

        var names = new List<string>();

        //foreach (var attachment in attachments)
        //{
        //    _logger.LogDebug("Downloading attachment: {Name}", attachment);

        //    var bytes = await _client.GetAttachmentById(attachment);
        //    names.Add(attachment.ToString());
        //}

        _logger.LogDebug("Ending downloading attachments: {@Names}", names);

        return names;
    }
}
