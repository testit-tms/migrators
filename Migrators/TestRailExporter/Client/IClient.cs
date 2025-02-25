using TestRailExporter.Models;

namespace TestRailExporter.Client;

public interface IClient
{
    Task<TestRailProject> GetProject();
    Task<List<TestRailSection>> GetSectionsByProjectId(int projectId);
    Task<List<TestRailSharedStep>> GetSharedStepIdsByProjectId(int projectId);
    Task<List<TestRailCase>> GetTestCaseIdsByProjectIdAndSectionId(int projectId, int sectionId);
    Task<List<TestRailAttachment>> GetAttachmentsByTestCaseId(int testCaseId);
    Task<byte[]> GetAttachmentById(int attachmentId);
}
