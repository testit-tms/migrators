using AllureExporter.Models;

namespace AllureExporter.Services;

public interface IAttachmentService
{
    Task<List<string>> DownloadAttachments(Guid id, IEnumerable<AllureAttachment> attachments);
}
