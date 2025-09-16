using ZephyrScaleServerExporter.Models.Attachment;
using ZephyrScaleServerExporter.Models.Client;
using ZephyrScaleServerExporter.Models.TestCases;

namespace ZephyrScaleServerExporter.Client;

public interface IClient
{
    Task<ZephyrProject> GetProject();
    Task<ZephyrProject> GetProjectCloud();
    Task<List<ZephyrStatus>> GetStatusesCloud(string projectKey);
    Task<List<CloudZephyrPriority>> GetPrioritiesCloud(string projectKey);
    Task<List<ZephyrStatus>> GetStatuses(string projectId);
    Task<List<ZephyrCustomFieldForTestCase>> GetCustomFieldsForTestCases(string projectId);
    Task<List<ZephyrTestCase>> GetTestCases(int startAt, int maxResults, string statuses);
    Task<List<CloudZephyrTestCase>> GetTestCasesCloud(int startAt, int maxResults, string statuses);
    Task<List<ZephyrTestCase>> GetTestCasesArchived(int startAt, int maxResults, string statuses);
    Task<List<ZephyrTestCaseRoot>> GetTestCasesNew(string statuses);
    Task<List<ZephyrTestCase>> GetTestCasesWithFilter(int startAt, int maxResults, string statuses, string filterName);
    Task<ZephyrTestCase> GetTestCase(string testCaseKey);
    Task<TraceLinksRoot?> GetTestCaseTraces(string testCaseKey);
    Task<TestCaseTracesResponseWrapper?> GetTestCaseTracesV2(string testCaseKey, bool isArchived);
    Task<ZephyrArchivedTestCase> GetArchivedTestCase(string testCaseKey);
    Task<ParametersData> GetParametersByTestCaseKey(string testCaseKey);
    Task<List<JiraComponent>> GetComponents();
    Task<JiraIssue> GetIssueById(string issueId);
    Task<List<ConfluencePageId>> GetConfluencePageIdsByTestCaseId(int testCaseId);
    Task<List<ZephyrConfluenceLink>> GetConfluenceLinksByConfluencePageId(string confluencePageId);
    Task<ZephyrOwner?> GetOwner(string ownerKey);
    Task<List<ZephyrAttachment>> GetAttachmentsForTestCase(string testCaseKey);

    Task<List<AltAttachmentResult>> GetAltAttachmentsForTestCase(string testCaseId);
    Task<byte[]> DownloadAttachment(string url, Guid testCaseId);
    Task<byte[]> DownloadAttachmentById(int id, Guid testCaseId);
    Uri GetBaseUrl();
}
