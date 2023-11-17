using PractiTestExporter.Client;
using Microsoft.Extensions.Logging;
using Models;
using JsonWriter;

namespace PractiTestExporter.Services;

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly IClient _client;
    private readonly IWriteService _writeService;
    private readonly ITestCaseService _testCaseService;
    private readonly IAttributeService _attributeService;
    private const string SectionName = "PractiTest";

    public ExportService(ILogger<ExportService> logger, IClient client,
        IWriteService writeService, ITestCaseService testCaseService, IAttributeService attributeService)
    {
        _logger = logger;
        _client = client;
        _writeService = writeService;
        _testCaseService = testCaseService;
        _attributeService = attributeService;
    }

    public async Task ExportProject()
    {
        _logger.LogInformation("Starting export");

        var project = await _client.GetProject();

        var section = new Section
        {
            Id = Guid.NewGuid(),
            Name = SectionName,
            PreconditionSteps = new List<Step>(),
            PostconditionSteps = new List<Step>(),
            Sections = new List<Section>()
        };

        var attributeData = await _attributeService.ConvertCustomAttributes();
        var testCaseData = await _testCaseService.ConvertTestCases(
            section.Id,
            attributeData.AttributeMap
        );

        foreach (var sharedStep in testCaseData.SharedSteps)
        {
            await _writeService.WriteSharedStep(sharedStep);
        }

        foreach (var testCase in testCaseData.TestCases)
        {
            await _writeService.WriteTestCase(testCase);
        }

        var mainJson = new Root
        {
            ProjectName = project.Data.Attributes.Name,
            Sections = new List<Section> { section },
            TestCases = testCaseData.TestCases
                .Select(t => t.Id)
                .ToList(),
            SharedSteps = testCaseData.SharedSteps
                .Select(s => s.Id)
                .ToList(),
            Attributes = attributeData.Attributes,
        };

        await _writeService.WriteMainJson(mainJson);

        _logger.LogInformation("Ending export");
    }
}
