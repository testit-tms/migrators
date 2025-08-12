using Models;
using ZephyrScaleServerExporter.Models.Attachment;
using ZephyrScaleServerExporter.Models.TestCases;

namespace ZephyrScaleServerExporter.Services;

public interface IAttachmentService
{
    Task<string> DownloadAttachment(Guid testCaseId, ZephyrAttachment attachment, bool isSharedAttachment);
    Task<string> DownloadAttachmentById(Guid testCaseId, StepAttachment attachment, bool isSharedAttachment);
    Task<List<string>> CopySharedAttachments(Guid targetId, Step step);
}
