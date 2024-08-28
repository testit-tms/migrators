using QaseExporter.Models;

namespace QaseExporter.Services;

public interface IAttachmentService
{
    Task<List<string>> DownloadAttachments(List<QaseAttachment> qaseAttachments, Guid workItemId);
}
