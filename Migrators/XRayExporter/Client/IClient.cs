using XRayExporter.Models;

namespace XRayExporter.Client;

public interface IClient
{
    Task<JiraProject> GetProject();
    Task<List<XrayFolder>> GetFolders();
    Task<List<XRayTest>> GetTestFromFolder(int folderId);
    Task<XRayTestFull> GetTest(string testKey);
    Task<JiraItem> GetItem(string link);
    Task<byte[]> DownloadAttachment(string link);
}
