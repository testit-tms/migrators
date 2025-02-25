using TestRailExporter.Models;

namespace TestRailExporter.Client;

public interface IClient
{
    Task<TestRailProject> GetProject();
    Task<List<TestRailSection>> GetSectionsByProjectId(int projectId);
    Task<List<TestRailSharedStep>> GetSharedStepIdsByProjectId(int projectId);
    //Task<TestRailSharedStep> GetSharedStepById(int sharedStepId);
    Task<List<TestRailCase>> GetTestCaseIdsByProjectIdAndSectionId(int projectId, int sectionId);
    //Task<TestRailCase> GetTestCaseById(int caseId);
    Task<List<TestRailAttachment>> GetAttachmentsByTestCaseId(int testCaseId);
    Task<byte[]> GetAttachmentById(int attachmentId);
}
