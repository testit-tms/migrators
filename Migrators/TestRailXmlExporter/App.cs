using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TestRailXmlExporter.Services;

namespace TestRailXmlExporter;

public class App(
    IConfiguration configuration,
    ILogger<App> logger,
    ImportService importService,
    ExportService exportService)
{
    public async Task RunAsync()
    {
        logger.LogInformation("Starting application");
        var filePath = configuration["xmlPath"];

        try
        {
            (var testRailsXmlSuite, var customAttributes) = await importService.ImportXmlAsync(filePath)
                .ConfigureAwait(false);
            await exportService.ExportProjectAsync(testRailsXmlSuite, customAttributes).ConfigureAwait(false);

            logger.LogInformation("Xml file '{filePath}' import success", filePath);
        }
        catch (Exception exception)
        {
            logger.LogError("Xml file '{filePath}' import failed: {exception}", filePath, exception);
        }

        logger.LogInformation("Ending application");
    }
}
