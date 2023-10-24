using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;
using XRayExporter.Client;

namespace XRayExporter.Services;

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly IClient _client;
    private readonly ISectionService _sectionService;
    private readonly ITestCaseService _testCaseService;
    private readonly IWriteService _writeService;

    public ExportService(ILogger<ExportService> logger, IClient client, ISectionService sectionService,
        ITestCaseService testCaseService, IWriteService writeService)
    {
        _logger = logger;
        _client = client;
        _sectionService = sectionService;
        _testCaseService = testCaseService;
        _writeService = writeService;
    }

    public async Task ExportProject()
    {
        _logger.LogInformation("Exporting project...");

        var project = await _client.GetProject();
        var sections = await _sectionService.ConvertSections();
        var testCases = await _testCaseService.ConvertTestCases(sections.SectionMap);

        foreach (var sharedStep in testCases.SharedSteps)
        {
            await _writeService.WriteSharedStep(sharedStep);
        }

        foreach (var testCase in testCases.TestCases)
        {
            await _writeService.WriteTestCase(testCase);
        }

        var root = new Root
        {
            ProjectName = project.Name,
            Attributes = testCases.Attributes,
            Sections = sections.Sections,
            SharedSteps = testCases.SharedSteps
                .Select(s => s.Id)
                .ToList(),
            TestCases = testCases.TestCases
                .Select(t => t.Id)
                .ToList()
        };

        await _writeService.WriteMainJson(root);

        _logger.LogInformation("Project exported");
    }
}
