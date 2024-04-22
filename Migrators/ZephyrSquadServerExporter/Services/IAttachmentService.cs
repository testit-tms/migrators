using ZephyrSquadServerExporter.Models;

namespace ZephyrSquadServerExporter.Services;

public interface IAttachmentService
{
    Task<List<string>> GetAttachmentsForIssue(Guid testCaseId, List<IssueAttachment> attachments);
    Task<string> GetAttachmentsForStep(Guid testCaseId, string issueId, string attachmentId, string attachmentName);
}
