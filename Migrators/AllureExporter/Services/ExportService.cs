using AllureExporter.Client;
using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;

namespace AllureExporter.Services;

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly IClient _client;
    private readonly IWriteService _writeService;
    private readonly ISectionService _sectionService;
    private readonly ISharedStepService _sharedStepService;
    private readonly ITestCaseService _testCaseService;
    private readonly IAttributeService _attributeService;

    public ExportService(ILogger<ExportService> logger, IClient client, IWriteService writeService,
        ISectionService sectionService, ISharedStepService sharedStepService, ITestCaseService testCaseService, IAttributeService attributeService)
    {
        _logger = logger;
        _client = client;
        _writeService = writeService;
        _sectionService = sectionService;
        _sharedStepService = sharedStepService;
        _testCaseService = testCaseService;
        _attributeService = attributeService;
    }

    public virtual async Task ExportProject()
    {
        _logger.LogInformation("Starting export");

        var project = await _client.GetProjectId();
        var section = await _sectionService.ConvertSection(project.Id);
        var attributes = await _attributeService.GetCustomAttributes(project.Id);

        var customAttributes = attributes.ToDictionary(k => k.Name, v => v.Id);
        var sharedSteps = await _sharedStepService.ConvertSharedSteps(project.Id, section.MainSection.Id, attributes);
        var sharedStepsMap = sharedSteps.ToDictionary(k => k.Key.ToString(), v => v.Value.Id);
        var testCases =
            await _testCaseService.ConvertTestCases(project.Id, sharedStepsMap, customAttributes, section.SectionDictionary);

        foreach (var sharedStep in sharedSteps)
        {
            await _writeService.WriteSharedStep(sharedStep.Value);
        }

        foreach (var testCase in testCases)
        {
            await _writeService.WriteTestCase(testCase);
        }

        var mainJson = new Root
        {
            ProjectName = project.Name,
            Sections = new List<Section> { section.MainSection },
            TestCases = testCases.Select(t => t.Id).ToList(),
            SharedSteps = sharedSteps.Values.Select(s => s.Id).ToList(),
            Attributes = attributes
        };

        await _writeService.WriteMainJson(mainJson);

        _logger.LogInformation("Ending export");
    }
}
