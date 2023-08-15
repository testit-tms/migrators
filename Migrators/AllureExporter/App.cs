using AllureExporter.Client;
using AllureExporter.Services;
using Microsoft.Extensions.Logging;

namespace AllureExporter;

public class App
{
    private readonly ILogger<App> _logger;
    private readonly ConvertService _convertService;

    public App(ILogger<App> logger,  ConvertService convertService)
    {
        _logger = logger;
        _convertService = convertService;
    }

    public void Run(string[] args)
    {
        _logger.LogInformation("Starting application");

        _convertService.ConvertMainJson().Wait();

        _logger.LogInformation("Ending application");
    }
}
