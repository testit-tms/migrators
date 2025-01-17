using AllureExporter.Models;

namespace AllureExporter.Client;

public interface IClient
{
    Task<BaseEntity> GetProjectId();
    Task<List<long>> GetTestCaseIdsFromMainSuite(long projectId);
    Task<List<long>> GetTestCaseIdsFromSuite(long projectId, long suiteId);
    Task<List<AllureSharedStep>> GetSharedStepsByProjectId(long projectId);
    Task<AllureTestCase> GetTestCaseById(long testCaseId);
    Task<List<AllureStep>> GetSteps(long testCaseId);
    Task<AllureStepsInfo> GetStepsInfoByTestCaseId(long testCaseId);
    Task<AllureSharedStepsInfo> GetStepsInfoBySharedStepId(long sharedStepId);
    Task<List<AllureAttachment>> GetAttachmentsByTestCaseId(long testCaseId);
    Task<List<AllureAttachment>> GetAttachmentsBySharedStepId(long sharedStepId);
    Task<List<AllureLink>> GetIssueLinks(long testCaseId);
    Task<List<AllureRelation>> GetRelations(long testCaseId);
    Task<TcCommentsSection> GetComments(long testCaseId);
    Task<List<BaseEntity>> GetSuites(long projectId);
    Task<byte[]> DownloadAttachmentForTestCase(long attachmentId);
    Task<byte[]> DownloadAttachmentForSharedStep(long attachmentId);
    Task<List<BaseEntity>> GetTestLayers();
    Task<List<BaseEntity>> GetCustomFieldNames(long projectId);
    Task<List<BaseEntity>> GetCustomFieldValues(long fieldId);
    Task<List<AllureCustomField>> GetCustomFieldsFromTestCase(long testCaseId);
}
