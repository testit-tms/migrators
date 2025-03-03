using Importer.Services;
using Microsoft.Extensions.Logging;

namespace Importer;

public class App(ILogger<App> logger, IImportService importService)
{
    public void Run(string[] args)
    {
        logger.LogInformation("Starting application");

        importService.ImportProject().Wait();

        logger.LogInformation("Ending application");
    }
}