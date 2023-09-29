using Microsoft.Extensions.Logging;
using ZephyrScaleExporter.Client;

namespace ZephyrScaleExporter.Services;

public class ExportService : IExportService
{
    private readonly IFolderService _folderService;
    private readonly ILogger<ExportService> _logger;
    private readonly IClient _client;

    public ExportService(ILogger<ExportService> logger, IClient client, IFolderService folderService)
    {
        _folderService = folderService;
        _logger = logger;
        _client = client;
    }

    public async Task ExportProject()
    {
        _logger.LogInformation("Exporting project...");

        var project = await _client.GetProject();
        var statuses = await _client.GetStatuses();
        var priorities = await _client.GetPriorities();

        var folders = await _folderService.ConvertSections();

        _logger.LogInformation("Export complete");
    }
}
