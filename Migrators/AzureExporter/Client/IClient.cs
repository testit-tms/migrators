using AzureExporter.Models;

namespace AzureExporter.Client;

public interface IClient
{
    Task<AzureProject> GetProject();
    Task<List<int>> GetWorkItemIds(string type);
    Task<AzureWorkItem> GetWorkItemById(int id);
    Task<List<string>> GetIterations(Guid projectId);
    Task<byte[]> GetAttachmentById(Guid id);
}
