using Importer.Services;
using Microsoft.Extensions.Logging;

namespace Importer;

public class App
{
    private readonly ILogger<App> _logger;
    private readonly IImportService _importService;

    public App(ILogger<App> logger, IImportService importService)
    {
        _logger = logger;
        _importService = importService;
    }

    public void Run(string[] args)
    {
        _logger.LogInformation("Starting application");

        _importService.ImportProject().Wait();

        _logger.LogInformation("Ending application");
    }
}
