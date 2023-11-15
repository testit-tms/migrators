using AllureExporter.Client;
using AllureExporter.Services;
using Microsoft.Extensions.Logging;

namespace AllureExporter;

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

        try
        {
            _exportService.ExportProject().Wait();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred during export");
            throw;
        }

        _logger.LogInformation("Ending application");
    }
}
