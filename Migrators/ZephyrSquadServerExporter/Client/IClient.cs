using ZephyrSquadServerExporter.Models;

namespace ZephyrSquadServerExporter.Client;

public interface IClient
{
    Task<JiraProject> GetProject();
    Task<string> GetZephyrIssueTypeIdByProjectId(string projectId);
    Task<JiraIssueCustomAttributes> GetCustomAttributesByProjectIdAndZephyrIssueTypeId(string projectId, string zephyrIssueTypeId);
    Task<List<ZephyrCycle>> GetCyclesByProjectIdAndVersionId(string projectId, string versionId);
    Task<List<ZephyrFolder>> GetFoldersByProjectIdAndVersionIdAndCycleId(string projectId, string versionId, string cycleId);
    Task<List<ZephyrExecution>> GetTestCasesFromCycle(string projectId, string versionId, string cycleId);
    Task<List<ZephyrExecution>> GetTestCasesFromFolder(string projectId, string versionId, string cycleId, string folderId);
    Task<List<ZephyrStep>> GetSteps(string issueId);
    Task<JiraIssue> GetIssueById(string issueId);
    Task<byte[]> GetAttachmentForIssueById(string fileId, string fileName);
    Task<byte[]> GetAttachmentForStepById(string fileId);
}
