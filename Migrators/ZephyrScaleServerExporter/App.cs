using Microsoft.Extensions.Logging;
using ZephyrScaleServerExporter.Services;

namespace ZephyrScaleServerExporter;

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
        _logger.LogInformation("Starting application");

        _exportService.ExportProject().Wait();

        _logger.LogInformation("Ending application");
    }
}
