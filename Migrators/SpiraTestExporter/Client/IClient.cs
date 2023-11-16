using SpiraTestExporter.Models;

namespace SpiraTestExporter.Client;

public interface IClient
{
    Task<SpiraProject> GetProject();
    Task<List<SpiraFolder>> GetFolders(int projectId);
    Task<List<SpiraTest>> GetTestFromFolder(int projectId, int folderId);
    Task<List<SpiraPriority>> GetPriorities(int projectTemplateId);
    Task<List<SpiraStatus>> GetStatuses(int projectTemplateId);
    Task<List<SpiraStep>> GetTestSteps(int projectId, int testCaseId);
    Task<List<SpiraTestCaseParameter>> GetSpiraParameters(int projectId, int testCaseId);
    Task<List<SpiraStepParameter>> GetStepParameters(int projectId, int testCaseId, int stepId);
    Task<List<SpiraAttachment>> GetAttachments(int projectId, int artifactTypeId, int artifactId);
    Task<byte[]> DownloadAttachment(int projectId, int attachmentId);
}
