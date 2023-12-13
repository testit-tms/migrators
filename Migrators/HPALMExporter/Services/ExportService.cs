using HPALMExporter.Client;
using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;

namespace HPALMExporter.Services;

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly IClient _client;
    private readonly IAttributeService _attributeService;
    private readonly ISectionService _sectionService;
    private readonly ITestCaseService _testCaseService;
    private readonly IWriteService _writeService;

    public ExportService(ILogger<ExportService> logger, IClient client, IAttributeService attributeService,
        ISectionService sectionService, ITestCaseService testCaseService, IWriteService writeService)
    {
        _logger = logger;
        _client = client;
        _attributeService = attributeService;
        _sectionService = sectionService;
        _testCaseService = testCaseService;
        _writeService = writeService;
    }

    public async Task ExportProject()
    {
        _logger.LogInformation("Export project from HP ALM");

        await _client.Auth();

        var attributes = await _attributeService.ConvertAttributes();
        var section = await _sectionService.ConvertSections();
        var testCases =
            await _testCaseService.ConvertTestCases(section.SectionMap,
                attributes.ToDictionary(a => a.Name, a => a.Id));

        foreach (var testCasesSharedStep in testCases.SharedSteps)
        {
            await _writeService.WriteSharedStep(testCasesSharedStep);
        }

        foreach (var testCasesTest in testCases.TestCases)
        {
            await _writeService.WriteTestCase(testCasesTest);
        }

        var root = new Root
        {
            ProjectName = _client.GetProjectName(),
            Sections = section.Sections,
            Attributes = attributes,
            SharedSteps = testCases.SharedSteps.Select(s => s.Id).ToList(),
            TestCases = testCases.TestCases.Select(t => t.Id).ToList()
        };

        await _writeService.WriteMainJson(root);

        _logger.LogInformation("Export project from HP ALM success");
    }
}
