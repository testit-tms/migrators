using AzureExporter.Client;
using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;

namespace AzureExporter.Services;

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly IClient _client;
    private readonly ITestCaseService _testCaseService;
    private readonly IWriteService _writeService;
    private readonly ISharedStepService _sharedStepService;
    private readonly IAttributeService _attributeService;

    private const string SectionName = "Azure DevOps";

    public ExportService(ILogger<ExportService> logger, IClient client, ITestCaseService testCaseService,
        IWriteService writeService, ISharedStepService sharedStepService, IAttributeService attributeService)
    {
        _logger = logger;
        _client = client;
        _testCaseService = testCaseService;
        _writeService = writeService;
        _sharedStepService = sharedStepService;
        _attributeService = attributeService;
    }

    public async Task ExportProject()
    {
        _logger.LogInformation("Starting export");

        var project = await _client.GetProject();
        var attributes = await _attributeService.GetCustomAttributes(project.Id);
        var attributeMap = attributes.ToDictionary(k => k.Name, v => v.Id);

        var section = new Section
        {
            Id = Guid.NewGuid(),
            Name = SectionName,
            PreconditionSteps = new List<Step>(),
            PostconditionSteps = new List<Step>(),
            Sections = new List<Section>()
        };

        var sharedSteps = await _sharedStepService.ConvertSharedSteps(project.Id, section.Id, attributeMap);
        var sharedStepsMap = sharedSteps.ToDictionary(k => k.Key, v => v.Value.Id);
        var testCases = await _testCaseService.ConvertTestCases(project.Id, sharedStepsMap, section.Id, attributeMap);

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
            Sections = new List<Section> { section },
            TestCases = testCases.Select(t => t.Id).ToList(),
            SharedSteps = sharedSteps.Values.Select(s => s.Id).ToList(),
            Attributes = attributes
        };

        await _writeService.WriteMainJson(mainJson);

        _logger.LogInformation("Ending export");
    }
}
