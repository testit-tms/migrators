using AllureExporter.Models;

namespace AllureExporter.Client;

public interface IClient
{
    Task<BaseEntity> GetProjectId();
    Task<List<int>> GetTestCaseIdsFromMainSuite(int projectId);
    Task<List<int>> GetTestCaseIdsFromSuite(int projectId, int suiteId);
    Task<List<AllureSharedStep>> GetSharedStepsByProjectId(int projectId);
    Task<AllureTestCase> GetTestCaseById(int testCaseId);
    Task<List<AllureStep>> GetSteps(int testCaseId);
    Task<AllureStepsInfo> GetStepsInfoByTestCaseId(int testCaseId);
    Task<AllureSharedStepsInfo> GetStepsInfoBySharedStepId(int sharedStepId);
    Task<List<AllureAttachment>> GetAttachmentsByTestCaseId(int testCaseId);
    Task<List<AllureAttachment>> GetAttachmentsBySharedStepId(int sharedStepId);
    Task<List<AllureLink>> GetLinks(int testCaseId);
    Task<List<BaseEntity>> GetSuites(int projectId);
    Task<byte[]> DownloadAttachmentForTestCase(int attachmentId);
    Task<byte[]> DownloadAttachmentForSharedStep(int attachmentId);
    Task<List<BaseEntity>> GetTestLayers();
    Task<List<BaseEntity>> GetCustomFieldNames(int projectId);
    Task<List<BaseEntity>> GetCustomFieldValues(int fieldId);
    Task<List<AllureCustomField>> GetCustomFieldsFromTestCase(int testCaseId);
}
