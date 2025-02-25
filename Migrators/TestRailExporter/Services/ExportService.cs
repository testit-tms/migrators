using TestRailExporter.Client;
using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;

namespace TestRailExporter.Services;

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly IClient _client;
    private readonly IWriteService _writeService;
    private readonly ISectionService _sectionService;
    private readonly ISharedStepService _sharedStepService;
    private readonly ITestCaseService _testCaseService;

    public ExportService(ILogger<ExportService> logger, IClient client, IWriteService writeService,
        ISectionService sectionService, ISharedStepService sharedStepService, ITestCaseService testCaseService)
    {
        _logger = logger;
        _client = client;
        _writeService = writeService;
        _sectionService = sectionService;
        _sharedStepService = sharedStepService;
        _testCaseService = testCaseService;
    }

    public virtual async Task ExportProject()
    {
        _logger.LogInformation("Starting export");

        var project = await _client.GetProject();
        var sectionsInfo = await _sectionService.ConvertSections(project.Id);
        var sharedStepsInfo = await _sharedStepService.ConvertSharedSteps(project.Id, sectionsInfo.MainSection.Id);
        var testCases = await _testCaseService.ConvertTestCases(project.Id, sharedStepsInfo.SharedStepsMap, sectionsInfo);

        foreach (var sharedStep in sharedStepsInfo.SharedSteps)
        {
            await _writeService.WriteSharedStep(sharedStep);
        }

        foreach (var testCase in testCases)
        {
            await _writeService.WriteTestCase(testCase);
        }

        var mainJson = new Root
        {
            ProjectName = project.Name,
            Sections = new List<Section> { sectionsInfo.MainSection },
            TestCases = testCases.Select(t => t.Id).ToList(),
            SharedSteps = sharedStepsInfo.SharedSteps.Select(s => s.Id).ToList(),
        };

        await _writeService.WriteMainJson(mainJson);

        _logger.LogInformation("Ending export");
    }
}
