using ZephyrScaleServerExporter.Models;

namespace ZephyrScaleServerExporter.Client;

public interface IClient
{
    Task<ZephyrProject> GetProject();
    Task<List<ZephyrTestCase>> GetTestCases();
    Task<ZephyrTestCase> GetTestCase(string testCaseKey);
    Task<ZephyrArchivedTestCase> GetArchivedTestCase(string testCaseKey);
    Task<List<JiraComponent>> GetComponents(string projectKey);
    Task<JiraIssue> GetIssueById(string issueId);
    Task<List<ZephyrAttachment>> GetAttachmentsForTestCase(string testCaseKey);
    Task<byte[]> DownloadAttachment(string url);
}
