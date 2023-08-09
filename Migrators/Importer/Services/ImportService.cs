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

    private Dictionary<Guid, TmsAttribute> _attributesMap = new();

    public ImportService(ILogger<ImportService> logger,
        IParserService parserService,
        IClient client,
        IAttributeService attributeService,
        ISectionService sectionService,
        ISharedStepService sharedStepService,
        ITestCaseService testCaseService)
    {
        _logger = logger;
        _parserService = parserService;
        _client = client;
        _attributeService = attributeService;
        _sectionService = sectionService;
        _sharedStepService = sharedStepService;
        _testCaseService = testCaseService;
    }

    public async Task ImportProject()
    {
        _logger.LogInformation("Importing project");

        var mainJsonResult = await _parserService.GetMainFile();

        _logger.LogInformation("Creating project {Name}", mainJsonResult.ProjectName);

        await _client.CreateProject(mainJsonResult.ProjectName);

        var sections = await _sectionService.ImportSections(mainJsonResult.Sections);

        _attributesMap = await _attributeService.ImportAttributes(mainJsonResult.Attributes);

        var sharedSteps = await _sharedStepService.ImportSharedSteps(mainJsonResult.SharedSteps, sections,
            _attributesMap);

        await _testCaseService.ImportTestCases(mainJsonResult.TestCases, sections, _attributesMap, sharedSteps);

        _logger.LogInformation("Project imported");
    }
}
