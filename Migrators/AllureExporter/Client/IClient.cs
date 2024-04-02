using AllureExporter.Models;

namespace AllureExporter.Client;

public interface IClient
{
    Task<BaseEntity> GetProjectId();
    Task<List<int>> GetTestCaseIdsFromMainSuite(int projectId);
    Task<List<int>> GetTestCaseIdsFromSuite(int projectId, int suiteId);
    Task<AllureTestCase> GetTestCaseById(int testCaseId);
    Task<List<AllureStep>> GetSteps(int testCaseId);
    Task<AllureStepsInfo> GetStepsInfo(int testCaseId);
    Task<List<AllureAttachment>> GetAttachments(int testCaseId);
    Task<List<AllureLink>> GetLinks(int testCaseId);
    Task<List<BaseEntity>> GetSuites(int projectId);
    Task<byte[]> DownloadAttachment(int attachmentId);
    Task<List<BaseEntity>> GetTestLayers();
    Task<List<BaseEntity>> GetCustomFieldNames(int projectId);
    Task<List<BaseEntity>> GetCustomFieldValues(int fieldId);
    Task<List<AllureCustomField>> GetCustomFieldsFromTestCase(int testCaseId);
}
