using ZephyrScaleServerExporter.Models;

namespace ZephyrScaleServerExporter.Services;

public interface IAttachmentService
{
    Task<string> DownloadAttachment(Guid id, ZephyrAttachment attachment);
}
