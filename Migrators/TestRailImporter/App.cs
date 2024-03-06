using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TestRailExporter.Services;

namespace TestRailExporter;

public class App
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<App> _logger;
    private readonly TestRailImportService _importService;
    private readonly TestRailExportService _exportService;

    public App(IConfiguration configuration, ILogger<App> logger, TestRailImportService importService,
        TestRailExportService exportService)
    {
        _configuration = configuration;
        _logger = logger;
        _importService = importService;
        _exportService = exportService;
    }

    public async Task RunAsync()
    {
        _logger.LogInformation("Starting application");
        var filePath = _configuration["xmlPath"]!;

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

        _logger.LogInformation("Ending application");
    }
}
