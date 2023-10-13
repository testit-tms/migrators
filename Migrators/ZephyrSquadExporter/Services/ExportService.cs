using Microsoft.Extensions.Logging;

namespace ZephyrSquadExporter.Services;

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly IFolderService _folderService;
    private readonly ITestCaseService _testCaseService;

    public ExportService(ILogger<ExportService> logger, IFolderService folderService, ITestCaseService testCaseService)
    {
        _logger = logger;
        _folderService = folderService;
        _testCaseService = testCaseService;
    }

    public async Task ExportProject()
    {
        _logger.LogInformation("Exporting project");

        var sections = await _folderService.GetSections();

        var testCases = await _testCaseService.ConvertTestCases(sections.SectionMap);

        _logger.LogInformation("Exporting project complete");
    }
}
