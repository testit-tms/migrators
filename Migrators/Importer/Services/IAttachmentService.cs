namespace Importer.Services;

public interface IAttachmentService
{
    Task<Dictionary<string, Guid>> GetAttachments(Guid workItemId, IEnumerable<string> attachments);
}