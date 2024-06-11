using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models;

namespace ZephyrScaleServerExporter.Services;

public class ExportService : IExportService
{
    private readonly IFolderService _folderService;
    private readonly IAttributeService _attributeService;
    private readonly ITestCaseService _testCaseService;
    private readonly IWriteService _writeService;
    private readonly ILogger<ExportService> _logger;
    private readonly IClient _client;

    public ExportService(ILogger<ExportService> logger, IClient client, IFolderService folderService,
        IAttributeService attributeService, ITestCaseService testCaseService, IWriteService writeService)
    {
        _folderService = folderService;
        _attributeService = attributeService;
        _testCaseService = testCaseService;
        _writeService = writeService;
        _logger = logger;
        _client = client;
    }

    public async Task ExportProject()
    {
        _logger.LogInformation("Exporting project");

        var project = await _client.GetProject();
        var folders = await _folderService.ConvertSections(project.Name);
        var attributes = await _attributeService.ConvertAttributes(project.Key);
        var testCases = await _testCaseService.ConvertTestCases(folders, attributes.AttributeMap);

        foreach (var testCase in testCases.TestCases)
        {
            await _writeService.WriteTestCase(testCase);
        }

        attributes.Attributes.AddRange(testCases.Attributes);

        var root = new Root
        {
            ProjectName = project.Name,
            Attributes = attributes.Attributes,
            Sections = new List<Section> { folders.MainSection },
            SharedSteps = new List<Guid>(),
            TestCases = testCases.TestCases.Select(t => t.Id).ToList()
        };

        await _writeService.WriteMainJson(root);

        _logger.LogInformation("Export complete");
    }
}
