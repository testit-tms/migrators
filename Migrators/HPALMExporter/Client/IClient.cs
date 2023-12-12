using HPALMExporter.Models;
using ImportHPALMToTestIT.Models.HPALM;

namespace HPALMExporter.Client;

public interface IClient
{
    Task Auth();
    Task<IEnumerable<HPALMFolder>> GetTestFolders(uint id);
    Task<IEnumerable<HPALMTest>> GetTests(int folderId, IEnumerable<string> attributes);
    Task<HPALMTest> GetTest(uint testId, IEnumerable<string> attributes);
    Task<IEnumerable<HPALMStep>> GetSteps(uint testId);
    Task<IEnumerable<HPALMAttachment>> GetAttachmentsFromTest(uint testId);
    Task<IEnumerable<HPALMAttachment>> GetAttachmentsFromStep(uint stepId);
    Task DownloadAttachment(uint id, string path);
    Task DownloadAttachment2(uint id, string path);
    Task DownloadAttachment3(uint id, string path, string testId, string attachName);
    Task<IEnumerable<HPALMParameter>> GetParameters(uint testId);
    Task<HPALMAttributes> GetTestAttributes();
    Task<HPALMLists> GetLists();
}
