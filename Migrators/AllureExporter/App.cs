using AllureExporter.Services;
using Microsoft.Extensions.Logging;

namespace AllureExporter;

public class App(ILogger<App> logger, IExportService exportService)
{
    public void Run(string[] args)
    {
        logger.LogInformation("Starting application");

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
