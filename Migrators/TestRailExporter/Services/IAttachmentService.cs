using TestRailExporter.Models.Commons;

namespace TestRailExporter.Services;

public interface IAttachmentService
{
    Task<AttachmentsInfo> DownloadAttachmentsByCaseId(int testCaseId, Guid id);
    Task<string> DownloadAttachmentById(string attachmentId, Guid id);
}
