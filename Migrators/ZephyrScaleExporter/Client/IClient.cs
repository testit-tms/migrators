using ZephyrScaleExporter.Models;

namespace ZephyrScaleExporter.Client;

public interface IClient
{
    Task<ZephyrProject> GetProject();
    Task<List<ZephyrStatus>> GetStatuses();
    Task<List<ZephyrPriority>> GetPriorities();
    Task<List<ZephyrFolder>> GetFolders();
    Task<List<ZephyrTestCase>> GetTestCases(int folderId);
    Task<List<ZephyrStep>> GetSteps(string testCaseKey);
    Task<ZephyrTestScript> GetTestScript(string testCaseKey);
    Task<byte[]> DownloadAttachment(string url);
}
