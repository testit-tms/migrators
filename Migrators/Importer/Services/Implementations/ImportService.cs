using Importer.Client;
using Importer.Models;
using Microsoft.Extensions.Logging;

namespace Importer.Services.Implementations;

internal class ImportService(
    ILogger<ImportService> logger,
    IParserService parserService,
    IClientAdapter clientAdapter,
    IAttributeService attributeService,
    ISectionService sectionService,
    ISharedStepService sharedStepService,
    ITestCaseService testCaseService,
    IProjectService projectService)
    : IImportService
{
    private Dictionary<Guid, TmsAttribute> _attributesMap = new();

    public async Task ImportProject()
    {
        logger.LogInformation("Importing project");

        var mainJsonResult = await parserService.GetMainFile();

        var projectId = await projectService.ImportProject(mainJsonResult.ProjectName);

        var sections = await sectionService.ImportSections(projectId, mainJsonResult.Sections);

        _attributesMap = await attributeService.ImportAttributes(projectId, mainJsonResult.Attributes);

        var sharedSteps = await sharedStepService.ImportSharedSteps(projectId, mainJsonResult.SharedSteps, sections,
            _attributesMap);

        var notImportedTestCasesNames = await testCaseService.ImportTestCases(projectId, mainJsonResult.TestCases, sections, _attributesMap,
            sharedSteps);

        logger.LogError("Not imported test cases:");
        foreach (var testCaseName in notImportedTestCasesNames)
        {
            logger.LogInformation($"\t{testCaseName}");
        }
        logger.LogInformation("Project imported");
    }
}
