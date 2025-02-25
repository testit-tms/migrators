using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TestRailExporter.Services;

namespace TestRailExporter;

public class App
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<App> _logger;
    private readonly IExportService _exportService;

    public App(IConfiguration configuration, ILogger<App> logger, IExportService exportService)
    {
        _configuration = configuration;
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
