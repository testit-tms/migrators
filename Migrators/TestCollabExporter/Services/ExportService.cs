using Microsoft.Extensions.Logging;
using TestCollabExporter.Client;

namespace TestCollabExporter.Services;

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
        _logger.LogInformation("Exporting project");

        var companies = await _client.GetCompany();
        var project = await _client.GetProject(companies);
        var customFields = await _client.GetCustomFields(project.CompanyId);



        throw new NotImplementedException();
    }
}
