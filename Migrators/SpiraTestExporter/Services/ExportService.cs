using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;
using SpiraTestExporter.Client;

namespace SpiraTestExporter.Services;

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly IClient _client;
    private readonly ISectionService _sectionService;
    private readonly IAttributeService _attributeService;
    private readonly ITestCaseService _testCaseService;
    private readonly IWriteService _writeService;

    public ExportService(ILogger<ExportService> logger, IClient client, ISectionService sectionService,
        IAttributeService attributeService, ITestCaseService testCaseService, IWriteService writeService)
    {
        _logger = logger;
        _client = client;
        _sectionService = sectionService;
        _attributeService = attributeService;
        _testCaseService = testCaseService;
        _writeService = writeService;
    }

    public async Task ExportProject()
    {
        _logger.LogInformation("Exporting project");

        var project = await _client.GetProject();

        var sectionData = await _sectionService.GetSections(project.Id);
        var attributeData = await _attributeService.GetAttributes(project.TemplateId);

        var testCaseData = await _testCaseService.ConvertTestCases(project.Id, sectionData.SectionMap,
            attributeData.PrioritiesMap, attributeData.StatusesMap, attributeData.AttributesMap);


        foreach (var sharedStep in testCaseData.SharedSteps)
        {
            await _writeService.WriteSharedStep(sharedStep);
        }

        foreach (var testCase in testCaseData.TestCases)
        {
            await _writeService.WriteTestCase(testCase);
        }

        var root = new Root
        {
            ProjectName = project.Name,
            Attributes = attributeData.Attributes,
            Sections = sectionData.Sections,
            SharedSteps = testCaseData.SharedSteps.Select(s => s.Id).ToList(),
            TestCases = testCaseData.TestCases.Select(t => t.Id).ToList()
        };

        await _writeService.WriteMainJson(root);

        _logger.LogInformation("Export completed");
    }
}
