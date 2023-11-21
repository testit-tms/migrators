using JsonWriter;
using Microsoft.Extensions.Logging;
using SpiraTestExporter.Client;
using SpiraTestExporter.Models;

namespace SpiraTestExporter.Services;

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

    public async Task<List<string>> GetAttachments(Guid testCaseId, int projectId, ArtifactType artifactType, int artifactId)
    {
        _logger.LogInformation("Get attachments for {ArtifactType} {ArtifactId}", artifactType, artifactId);

        var attachments = await _client.GetAttachments(projectId, GetArtifactTypeId(artifactType), artifactId);
        var attachmentNames = new List<string>();

        foreach (var attachment in attachments)
        {
            var content = await _client.DownloadAttachment(projectId, attachment.Id);
            var attachmentName = await _writeService.WriteAttachment(testCaseId, content, attachment.Name);

            attachmentNames.Add(attachmentName);
        }

        _logger.LogDebug("Attachments: {@AttachmentNames}", attachmentNames);

        return attachmentNames;
    }

    private static int GetArtifactTypeId(ArtifactType artifactType)
    {
        return artifactType switch
        {
            ArtifactType.TestCase => 1,
            ArtifactType.Step => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(artifactType), artifactType, null)
        };
    }
}
