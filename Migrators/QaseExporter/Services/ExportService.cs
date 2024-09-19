using QaseExporter.Client;
using Microsoft.Extensions.Logging;
using Models;
using JsonWriter;

namespace QaseExporter.Services;

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly IClient _client;
    private readonly IWriteService _writeService;
    private readonly ISectionService _sectionService;
    private readonly ITestCaseService _testCaseService;
    private readonly ISharedStepService _sharedStepService;
    private readonly IAttributeService _attributeService;

    public ExportService(ILogger<ExportService> logger, IClient client, IWriteService writeService,
        ISectionService sectionService, ITestCaseService testCaseService, ISharedStepService sharedStepService,
        IAttributeService attributeService)
    {
        _logger = logger;
        _client = client;
        _writeService = writeService;
        _sectionService = sectionService;
        _testCaseService = testCaseService;
        _sharedStepService = sharedStepService;
        _attributeService = attributeService;
        _attributeService = attributeService;
    }

    public async Task ExportProject()
    {
        _logger.LogInformation("Starting export");

        var project = await _client.GetProject();
        var sectionData = await _sectionService.ConvertSections();
        var sharedSteps = await _sharedStepService.ConvertSharedSteps(sectionData.MainSection.Id);
        var attributes = await _attributeService.ConvertAttributes();
        var testCases = await _testCaseService.ConvertTestCases(sectionData.SectionMap, sharedSteps, attributes);

        foreach (var testCase in testCases)
        {
            await _writeService.WriteTestCase(testCase);
        }

        foreach (var sharedStep in sharedSteps)
        {
            await _writeService.WriteSharedStep(sharedStep.Value);
        }

        var mainJson = new Root
        {
            ProjectName = project.Name,
            Sections = new List<Section> { sectionData.MainSection },
            TestCases = testCases.Select(t => t.Id).ToList(),
            SharedSteps = sharedSteps.Values.Select(s => s.Id).ToList(),
            Attributes = attributes.Attributes,
        };

        await _writeService.WriteMainJson(mainJson);

        _logger.LogInformation("Ending export");
    }
}
