using ZephyrSquadExporter.Models;

namespace ZephyrSquadExporter.Client;

public interface IClient
{
    Task<List<ZephyrCycle>> GetCycles();
    Task<List<ZephyrFolder>> GetFolders(string cycleId);
    Task<List<ZephyrExecution>> GetTestCasesFromCycle(string cycleId);
    Task<List<ZephyrExecution>> GetTestCasesFromFolder(string cycleId, string folderId);
    Task<List<ZephyrStep>> GetSteps(string issueId);
    Task<List<ZephyrAttachment>> GetAttachmentsFromExecution(string issueId, string entityId);
    Task<byte[]> GetAttachmentFromExecution(string issueId, string entityId);
}
