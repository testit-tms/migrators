namespace ZephyrSquadExporter.Services;

public interface IAttachmentService
{
    Task<List<string>> GetAttachmentsFromExecution(Guid testCaseId, string issueId, string entityId);
    Task<string> GetAttachmentsFromStep(Guid testCaseId, string issueId, string attachmentId, string attachmentName);
}
