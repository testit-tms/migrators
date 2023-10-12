using ZephyrSquadExporter.Models;

namespace ZephyrSquadExporter.Client;

public interface IClient
{
    Task<List<ZephyrCycle>> GetCycles();
    Task<List<ZephyrFolder>> GetFolders(string cycleId);
    Task<List<ZephyrExecution>> GetTestCases(string storageId, bool isFolder = false);
    Task<List<ZephyrStep>> GetSteps(string issueId);
}
