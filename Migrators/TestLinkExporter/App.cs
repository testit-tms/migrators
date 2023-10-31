using Microsoft.Extensions.Logging;
using TestLinkExporter.Services;

namespace TestLinkExporter;

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

        _service.ExportProject().Wait();

        _logger.LogInformation("Ending application");
    }
}
