using DoqaExporter.Models;

namespace DoqaExporter.Client;

public interface IDoqaClient
{
    Task<string> Login();
    Task<string> GetFileBaseUrl();
    Task<List<DoqaProject>> GetProjects();
    Task<DoqaFolderTree> GetFolders(int spaceId);
    Task<List<int>> GetAllCaseIds(int spaceId);
    Task<DoqaTestCase> GetCase(int caseId);
    Task<byte[]?> DownloadAttachment(string path);
    Task<DoqaFolderTree> GetChecklistFolders(int spaceId);
    Task<List<int>> GetAllChecklistIds(int spaceId);
    Task<DoqaChecklist> GetChecklist(int checklistId);
}
