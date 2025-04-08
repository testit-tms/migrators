using TestRailExporter.Models.Client;

namespace TestRailExporter.Client;

public interface IClient
{
    Task<TestRailProject> GetProject();
    Task<List<TestRailSuite>> GetSuitesByProjectId(int projectId);
    Task<List<TestRailSection>> GetSectionsByProjectId(int projectId);
    Task<List<TestRailSection>> GetSectionsByProjectIdAndSuiteId(int projectId, int suiteId);
    Task<List<TestRailSharedStep>> GetSharedStepIdsByProjectId(int projectId);
    Task<List<TestRailCase>> GetTestCaseIdsByProjectIdAndSectionId(int projectId, int sectionId);
    Task<List<TestRailCase>> GetTestCaseIdsByProjectIdAndSuiteIdAndSectionId(int projectId, int suiteId, int sectionId);
    Task<List<TestRailAttachment>> GetAttachmentsByTestCaseId(int testCaseId);
    Task<byte[]> GetAttachmentById(int attachmentId);
    Task<byte[]> GetAttachmentByUrl(string url);
}
