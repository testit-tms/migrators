using Importer.Client;
using Microsoft.Extensions.Logging;

namespace Importer.Services.Implementations;

internal class AttachmentService(
    ILogger<AttachmentService> logger,
    IClientAdapter clientAdapter,
    IParserService parserService)
    : IAttachmentService
{
    public async Task<Dictionary<string, Guid>> GetAttachments(Guid workItemId, IEnumerable<string> attachments)
    {
        logger.LogInformation("Importing attachments for work item {Id}", workItemId);

        Dictionary<string, Guid> ids = new();

        foreach (var attachment in attachments)
        {
            var stream = await parserService.GetAttachment(workItemId, attachment);
            var id = await clientAdapter.UploadAttachment(Path.GetFileName(stream.Name), stream);

            ids.Add(attachment, id);
        }

        return ids;
    }
}