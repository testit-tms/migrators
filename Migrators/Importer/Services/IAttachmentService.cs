namespace Importer.Services;

public interface IAttachmentService
{
    Task<string[]> GetAttachments(Guid workItemId, IEnumerable<string> attachments);
}
