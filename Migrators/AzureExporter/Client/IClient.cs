//using AzureExporter.Models;

using AzureExporter.Models;

namespace AzureExporter.Client;

public interface IClient
{
    Task<Wiql> GetWorkItems();
    Task<TestCase> GetWorkItemById(int id);
    Task<string> GetAttachmentById(int id);
}
