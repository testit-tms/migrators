using Importer.Client;
using Importer.Models;
using Microsoft.Extensions.Logging;

namespace Importer.Services;

public class ImportService : IImportService
{
    private readonly ILogger<ImportService> _logger;
    private readonly IParserService _parserService;
    private readonly IClient _client;
    private readonly IAttributeService _attributeService;
    private readonly ISectionService _sectionService;
    private readonly ISharedStepService _sharedStepService;
    private readonly ITestCaseService _testCaseService;
    private readonly IProjectService _projectService;

    private Dictionary<Guid, TmsAttribute> _attributesMap = new();

    public ImportService(ILogger<ImportService> logger,
        IParserService parserService,
        IClient client,
        IAttributeService attributeService,
        ISectionService sectionService,
        ISharedStepService sharedStepService,
        ITestCaseService testCaseService,
        IProjectService projectService)
    {
        _logger = logger;
        _parserService = parserService;
        _client = client;
        _attributeService = attributeService;
        _sectionService = sectionService;
        _sharedStepService = sharedStepService;
        _testCaseService = testCaseService;
        _projectService = projectService;
    }

    public async Task ImportProject()
    {
        _logger.LogInformation("Importing project");

        var mainJsonResult = await _parserService.GetMainFile();

        var projectId = await _projectService.ImportProject(mainJsonResult.ProjectName);

        var sections = await _sectionService.ImportSections(projectId, mainJsonResult.Sections);

        _attributesMap = await _attributeService.ImportAttributes(projectId, mainJsonResult.Attributes);

        var sharedSteps = await _sharedStepService.ImportSharedSteps(projectId, mainJsonResult.SharedSteps, sections,
            _attributesMap);

        await _testCaseService.ImportTestCases(projectId, mainJsonResult.TestCases, sections, _attributesMap, sharedSteps);

        _logger.LogInformation("Project imported");
    }
}
