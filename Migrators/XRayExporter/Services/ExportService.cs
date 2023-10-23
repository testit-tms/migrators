using Microsoft.Extensions.Logging;
using XRayExporter.Client;

namespace XRayExporter.Services;

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly IClient _client;

    public ExportService(ILogger<ExportService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task ExportProject()
    {
        _logger.LogInformation("Exporting project...");

        var project = await _client.GetProject();

        _logger.LogInformation("Project exported.");
    }
}
