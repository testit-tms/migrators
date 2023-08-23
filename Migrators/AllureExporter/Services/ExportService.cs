using System.Text;
using AllureExporter.Client;
using AllureExporter.Models;
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
        var testCases = await _testCaseService.ConvertTestCase(project.Id, _attributeId, section.SectionDictionary);

        testCases.ForEach(t => _writeService.WriteTestCase(t));

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
                    Name = "AllureStatus",
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
                }
            }
        };

        await _writeService.WriteMainJson(mainJson);

        _logger.LogInformation("Ending export");
    }
}
