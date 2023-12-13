using HPALMExporter.Models;
using ImportHPALMToTestIT.Models.HPALM;

namespace HPALMExporter.Client;

public interface IClient
{
    string GetProjectName();
    Task Auth();
    Task<List<HPALMFolder>> GetTestFolders(uint id);
    Task<List<HPALMTest>> GetTests(int folderId, IEnumerable<string> attributes);
    Task<HPALMTest> GetTest(int testId, IEnumerable<string> attributes);
    Task<List<HPALMStep>> GetSteps(int testId);
    Task<List<HPALMAttachment>> GetAttachmentsFromTest(int testId);
    Task<List<HPALMAttachment>> GetAttachmentsFromStep(int stepId);
    Task<byte[]> DownloadAttachment(int testId, string attachName);
    Task<List<HPALMParameter>> GetParameters(int testId);
    Task<HPALMAttributes> GetTestAttributes();
    Task<HPALMLists> GetLists();
}
