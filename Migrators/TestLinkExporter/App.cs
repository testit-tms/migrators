using Microsoft.Extensions.Logging;
using Models;
using TestLinkExporter.Services;

namespace TestLinkExporter;

public class App(ILogger<App> logger, IExportService service)
{
    public void Run(string[] args)
    {
        logger.LogInformation("Starting application. Version: {Version}", AppVersion.Current);

        service.ExportProject().Wait();

        logger.LogInformation("Ending application");
    }
}
