using ZephyrScaleExporter.Models;

namespace ZephyrScaleExporter.Services;

public interface IAttachmentService
{
    Task<string> DownloadAttachment(Guid id, ZephyrAttachment attachment);
    Task<List<string>> DownloadAttachments(Guid id, List<ZephyrAttachment> attachments);
}
