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
            try
            {
                var stream = await parserService.GetAttachment(workItemId, attachment);
                var id = await clientAdapter.UploadAttachment(Path.GetFileName(stream.Name), stream);

                ids.Add(attachment, id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to upload attachment, skip: {Attachment}", attachment);
                // just skip this attachment and go next
            }
        }
        logger.LogInformation("Complete GetAttachments with {Count} attachments", ids.Count);

        return ids;
    }
}