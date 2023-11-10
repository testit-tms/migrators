using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;
using TestCollabExporter.Client;

namespace TestCollabExporter.Services;

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly IClient _client;
    private readonly ISectionService _sectionService;
    private readonly ITestCaseService _testCaseService;
    private readonly ISharedStepService _sharedStepService;
    private readonly IAttributeService _attributeService;
    private readonly IWriteService _writeService;

    public ExportService(ILogger<ExportService> logger, IClient client, ISectionService sectionService,
        ITestCaseService testCaseService, ISharedStepService sharedStepService, IAttributeService attributeService,
        IWriteService writeService)
    {
        _logger = logger;
        _client = client;
        _sectionService = sectionService;
        _testCaseService = testCaseService;
        _sharedStepService = sharedStepService;
        _attributeService = attributeService;
        _writeService = writeService;
    }

    public async Task ExportProject()
    {
        _logger.LogInformation("Exporting project");

        var companies = await _client.GetCompany();
        var project = await _client.GetProject(companies);

        var attributes = await _attributeService.ConvertAttributes(project.CompanyId);
        var sections = await _sectionService.ConvertSections(project.Id);
        var sharedSteps = await _sharedStepService.ConvertSharedSteps(project.Id, sections.SharedStepSection.Id,
            attributes.AttributesMap.Values.ToList());
        var testCases = await _testCaseService.ConvertTestCases(project.Id, sections.SectionMap,
            attributes.AttributesMap, sharedSteps.SharedStepsMap);

        foreach (var sharedStep in sharedSteps.SharedSteps)
        {
            await _writeService.WriteSharedStep(sharedStep);
        }

        foreach (var testCase in testCases)
        {
            await _writeService.WriteTestCase(testCase);
        }

        sections.Sections.Add(sections.SharedStepSection);

        var root = new Root
        {
            ProjectName = project.Name,
            Attributes = attributes.Attributes,
            Sections = sections.Sections,
            TestCases = testCases.Select(t => t.Id).ToList(),
            SharedSteps = sharedSteps.SharedSteps.Select(s => s.Id).ToList()
        };

        await _writeService.WriteMainJson(root);

        _logger.LogInformation("Exporting project completed");
    }
}
