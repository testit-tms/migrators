using PractiTestExporter.Models;

namespace PractiTestExporter.Services;

public interface IAttachmentService
{
    Task<List<string>> DownloadAttachments(string entityType, string id, Guid workItemId);
}
