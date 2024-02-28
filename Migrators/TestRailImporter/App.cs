using Microsoft.Extensions.Logging;
using TestRailImporter.Services;

namespace TestRailImporter;

public class App
{
    private readonly ILogger<App> _logger;
    private readonly TestRailImportService _importService;
    private readonly TestRailExportService _exportService;

    public App(ILogger<App> logger, TestRailImportService importService, TestRailExportService exportService)
    {
        _logger = logger;
        _importService = importService;
        _exportService = exportService;
    }

    public async Task RunAsync(string[] args)
    {
        _logger.LogInformation("Starting application");

        foreach (var filePath in args)
        {
            try
            {
                (var testRailsXmlSuite, var customAttributes) = await _importService.ImportXmlAsync(filePath)
                    .ConfigureAwait(false);
                await _exportService.ExportProjectAsync(testRailsXmlSuite, customAttributes).ConfigureAwait(false);

                _logger.LogInformation("Xml file '{filePath}' import success", filePath);
            }
            catch (Exception exception)
            {
                _logger.LogError("Xml file '{filePath}' import failed: {exception}", filePath, exception);
            }
        }

        _logger.LogInformation("Ending application");
    }
}
