namespace Importer.Services;

public interface IAttachmentService
{
    Task<List<string>> GetAttachments(Guid workItemId, IEnumerable<string> attachments);
}
