using JsonWriter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;
using Attribute = Models.Attribute;

namespace ZephyrSquadExporter.Services;

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly IFolderService _folderService;
    private readonly ITestCaseService _testCaseService;
    private readonly IWriteService _writeService;
    private readonly string _projectName;

    public ExportService(ILogger<ExportService> logger, IFolderService folderService, ITestCaseService testCaseService,
        IWriteService writeService, IConfiguration configuration)
    {
        _logger = logger;
        _folderService = folderService;
        _testCaseService = testCaseService;
        _writeService = writeService;

        var section = configuration.GetSection("zephyr");
        var projectName = section["projectName"];
        if (string.IsNullOrEmpty(projectName))
        {
            throw new ArgumentException("Project name is not specified");
        }

        _projectName = projectName;
    }


    public async Task ExportProject()
    {
        _logger.LogInformation("Exporting project");

        var sections = await _folderService.GetSections();
        var testCases = await _testCaseService.ConvertTestCases(sections.SectionMap);

        foreach (var testCase in testCases)
        {
            await _writeService.WriteTestCase(testCase);
        }

        var root = new Root
        {
            ProjectName = _projectName,
            TestCases = testCases.Select(t => t.Id).ToList(),
            SharedSteps = new List<Guid>(),
            Attributes = new List<Attribute>(),
            Sections = sections.Sections
        };

        await _writeService.WriteMainJson(root);

        _logger.LogInformation("Exporting project complete");
    }
}
