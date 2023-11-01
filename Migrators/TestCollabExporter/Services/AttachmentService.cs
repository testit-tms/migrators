using JsonWriter;
using Microsoft.Extensions.Logging;
using TestCollabExporter.Client;

namespace TestCollabExporter.Services;

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

    public async Task<string> DownloadAttachment(Guid testCase, string link, string filename)
    {
        _logger.LogInformation("Downloading attachment {Filename}", filename);

        var bytes = await _client.DownloadAttachment(link);

        var name = await _writeService.WriteAttachment(testCase, bytes, filename);

        return name;
    }
}
