using AzureExporter.Client;
using AzureExporter.Services;
using Microsoft.Extensions.Logging;

namespace AzureExporter;

public class App
{
    private readonly ILogger<App> _logger;
    private readonly IExportService _service;

    public App(ILogger<App> logger, IExportService service)
    {
        _logger = logger;
        _service = service;
    }

    public void Run(string[] args)
    {
        _logger.LogInformation("Starting application");

        try
        {
            _service.ExportProject().Wait();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred during export");
            throw;
        }

        _logger.LogInformation("Ending application");
    }
}
