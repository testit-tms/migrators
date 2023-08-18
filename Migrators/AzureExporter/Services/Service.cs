using AzureExporter.Client;
using AzureExporter.Models;
using Microsoft.Extensions.Logging;

namespace AzureExporter.Services;

public class Service : IService
{
    private readonly ILogger<Service> _logger;
    private readonly IClient _client;

    public Service(ILogger<Service> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task Export()
    {
        _logger.LogInformation("Export");

        Wiql content = await _client.GetWorkItems();

        foreach (WorkItem workItem in content.workItems) {
            await _client.GetWorkItemById(workItem.id);
        }

        _logger.LogInformation("Exported");
    }
}
