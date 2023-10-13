using ZephyrSquadExporter.Models;

namespace ZephyrSquadExporter.Client;

public interface IClient
{
    Task<List<ZephyrCycle>> GetCycles();
    Task<List<ZephyrFolder>> GetFolders(string cycleId);
    Task<List<ZephyrExecution>> GetTestCasesFromCycle(string cycleId);
    Task<List<ZephyrExecution>> GetTestCasesFromFolder(string cycleId, string folderId);
    Task<List<ZephyrStep>> GetSteps(string issueId);
}
