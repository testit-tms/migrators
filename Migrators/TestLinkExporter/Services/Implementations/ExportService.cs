using TestLinkExporter.Client;
using Microsoft.Extensions.Logging;
using Models;
using JsonWriter;

namespace TestLinkExporter.Services.Implementations;

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly IClient _client;
    private readonly IWriteService _writeService;
    private readonly ISectionService _sectionService;
    private readonly ITestCaseService _testCaseService;
    private readonly IAttributeService _attributeService;

    public ExportService(ILogger<ExportService> logger, IClient client, IWriteService writeService,
        ISectionService sectionService, ITestCaseService testCaseService, IAttributeService attributeService)
    {
        _logger = logger;
        _client = client;
        _writeService = writeService;
        _sectionService = sectionService;
        _testCaseService = testCaseService;
        _attributeService = attributeService;
    }

    public async Task ExportProject()
    {
        _logger.LogInformation("Starting export");

        var project = _client.GetProject();

        var attributes = _attributeService.GetCustomAttributes();
        var customAttributes = attributes.ToDictionary(k => k.Name, v => v.Id);

        var sectionData = _sectionService.ConvertSections(project.Id);

        var testCases = await _testCaseService.ConvertTestCases(sectionData.SectionMap, customAttributes);

        foreach (var testCase in testCases)
        {
            await _writeService.WriteTestCase(testCase);
        }

        var mainJson = new Root
        {
            ProjectName = project.Name,
            Sections = sectionData.Sections,
            TestCases = testCases.Select(t => t.Id).ToList(),
            SharedSteps = new List<Guid>(),
            Attributes = attributes,
        };

        await _writeService.WriteMainJson(mainJson);

        _logger.LogInformation("Ending export");
    }
}
