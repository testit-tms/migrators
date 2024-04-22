using JsonWriter;
using ZephyrSquadServerExporter.Client;
using Microsoft.Extensions.Logging;
using Models;
using Attribute = Models.Attribute;

namespace ZephyrSquadServerExporter.Services;

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly IClient _client;
    private readonly IFolderService _folderService;
    private readonly ITestCaseService _testCaseService;
    private readonly IWriteService _writeService;

    public ExportService(ILogger<ExportService> logger, IClient client, IFolderService folderService, ITestCaseService testCaseService,
        IWriteService writeService)
    {
        _logger = logger;
        _client = client;
        _folderService = folderService;
        _testCaseService = testCaseService;
        _writeService = writeService;
    }


    public async Task ExportProject()
    {
        _logger.LogInformation("Exporting project");

        var zephyrProject = await _client.GetProject();

        var sections = await _folderService.GetSections(zephyrProject.Versions, zephyrProject.Id);
        var testCases = await _testCaseService.ConvertTestCases(sections.AllSections);

        foreach (var testCase in testCases)
        {
            await _writeService.WriteTestCase(testCase);
        }

        var root = new Root
        {
            ProjectName = zephyrProject.Name,
            TestCases = testCases.Select(t => t.Id).ToList(),
            SharedSteps = new List<Guid>(),
            Attributes = new List<Attribute>(),
            Sections = sections.SectionsTree
        };

        await _writeService.WriteMainJson(root);

        _logger.LogInformation("Exporting project complete");
    }
}
