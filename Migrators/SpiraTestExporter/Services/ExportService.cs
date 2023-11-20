using Microsoft.Extensions.Logging;
using SpiraTestExporter.Client;

namespace SpiraTestExporter.Services;

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly IClient _client;
    private readonly ISectionService _sectionService;
    private readonly IAttributeService _attributeService;

    public ExportService(ILogger<ExportService> logger, IClient client, ISectionService sectionService, IAttributeService attributeService)
    {
        _logger = logger;
        _client = client;
        _sectionService = sectionService;
        _attributeService = attributeService;
    }

    public async Task ExportProject()
    {
        _logger.LogInformation("Exporting project");

        var project = await _client.GetProject();

        var sectionData = await _sectionService.GetSections(project.Id);
        var attributeData = await _attributeService.GetAttributes(project.TemplateId);


    }
}
