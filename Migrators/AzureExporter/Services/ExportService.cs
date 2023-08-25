using AzureExporter.Client;
using AzureExporter.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using Models;

namespace AzureExporter.Services;

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly IClient _client;
    private readonly ITestCaseService _testCaseService;

    public ExportService(ILogger<ExportService> logger, IClient client, ITestCaseService testCaseService)
    {
        _logger = logger;
        _client = client;
        _testCaseService = testCaseService;
    }

    public async Task ExportProject()
    {
        _logger.LogInformation("Export");

        _testCaseService.Export();

        _logger.LogInformation("Exported");
    }
}
