using AllureExporter.Services;
using Microsoft.Extensions.Logging;
using Models;

namespace AllureExporter;

public class App(ILogger<App> logger, IExportService exportService)
{
    public void Run(string[] args)
    {
        logger.LogInformation("Starting application. Version: {Version}", AppVersion.Current);

        try
        {
            exportService.ExportProject().Wait();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred during export");
            throw;
        }

        logger.LogInformation("Ending application");
    }
}
