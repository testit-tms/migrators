using Microsoft.Extensions.Logging;
using Models;
using ZephyrSquadExporter.Services;

namespace ZephyrSquadExporter;

public class App
{
    private readonly ILogger<App> _logger;
    private readonly IExportService _exportService;

    public App(ILogger<App> logger,  IExportService exportService)
    {
        _logger = logger;
        _exportService = exportService;
    }

    public void Run(string[] args)
    {
        _logger.LogInformation("Starting application. Version: {Version}", AppVersion.Current);

        _exportService.ExportProject().Wait();

        _logger.LogInformation("Ending application");
    }
}
