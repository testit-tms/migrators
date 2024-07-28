using ZephyrScaleServerExporter.Models;

namespace ZephyrScaleServerExporter.Client;

public interface IClient
{
    Task<ZephyrProject> GetProject();
    Task<List<ZephyrCustomFieldForTestCase>> GetCustomFieldsForTestCases(string projectId);
    Task<List<ZephyrTestCase>> GetTestCases();
    Task<ZephyrTestCase> GetTestCase(string testCaseKey);
    Task<ZephyrArchivedTestCase> GetArchivedTestCase(string testCaseKey);
    Task<ParametersData> GetParametersByTestCaseKey(string testCaseKey);
    Task<List<JiraComponent>> GetComponents();
    Task<JiraIssue> GetIssueById(string issueId);
    Task<List<ZephyrAttachment>> GetAttachmentsForTestCase(string testCaseKey);
    Task<byte[]> DownloadAttachment(string url);
}
