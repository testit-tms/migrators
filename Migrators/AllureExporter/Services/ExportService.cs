using AllureExporter.Client;
using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;
using Attribute = Models.Attribute;

namespace AllureExporter.Services;

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly IClient _client;
    private readonly IWriteService _writeService;
    private readonly ISectionService _sectionService;
    private readonly ITestCaseService _testCaseService;
    private readonly Guid _attributeId = Guid.NewGuid();
    private readonly Guid _layerId = Guid.NewGuid();

    private const string AllureStatus = "AllureStatus";
    private const string AllureTestLayer = "Test Layer";

    public ExportService(ILogger<ExportService> logger, IClient client, IWriteService writeService,
        ISectionService sectionService, ITestCaseService testCaseService)
    {
        _logger = logger;
        _client = client;
        _writeService = writeService;
        _sectionService = sectionService;
        _testCaseService = testCaseService;
    }

    public virtual async Task ExportProject()
    {
        _logger.LogInformation("Starting export");

        var project = await _client.GetProjectId();
        var section = await _sectionService.ConvertSection(project.Id);
        var testCases =
            await _testCaseService.ConvertTestCases(project.Id, _attributeId, _layerId, section.SectionDictionary);

        testCases.ForEach(t => _writeService.WriteTestCase(t));

        var testLayers = await _client.GetTestLayers();

        var mainJson = new Root
        {
            ProjectName = project.Name,
            Sections = new List<Section> { section.MainSection },
            TestCases = testCases.Select(t => t.Id).ToList(),
            SharedSteps = new List<Guid>(),
            Attributes = new List<Attribute>
            {
                new()
                {
                    Id = _attributeId,
                    Name = AllureStatus,
                    IsActive = true,
                    IsRequired = true,
                    Type = AttributeType.Options,
                    Options = new List<string>
                    {
                        "Draft",
                        "Active",
                        "Outdated",
                        "Review"
                    }
                },
                new()
                {
                    Id = _layerId,
                    Name = AllureTestLayer,
                    IsActive = true,
                    IsRequired = true,
                    Type = AttributeType.Options,
                    Options = testLayers.Select(l => l.Name).ToList()
                }
            }
        };

        await _writeService.WriteMainJson(mainJson);

        _logger.LogInformation("Ending export");
    }
}
