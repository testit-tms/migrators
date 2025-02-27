using TestRailExporter.Client;
using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;

namespace TestRailExporter.Services.Implementations;

public class ExportService(
    ILogger<ExportService> logger,
    IClient client,
    IWriteService writeService,
    ISectionService sectionService,
    ISharedStepService sharedStepService,
    ITestCaseService testCaseService)
    : IExportService
{
    public virtual async Task ExportProject()
    {
        logger.LogInformation("Starting export");

        var project = await client.GetProject();
        var sectionsInfo = await sectionService.ConvertSections(project.Id);
        var sharedStepsInfo = await sharedStepService.ConvertSharedSteps(project.Id, sectionsInfo.MainSection.Id);
        var testCases = await testCaseService.ConvertTestCases(project.Id, sharedStepsInfo.SharedStepsMap, sectionsInfo);

        foreach (var sharedStep in sharedStepsInfo.SharedSteps)
        {
            await writeService.WriteSharedStep(sharedStep);
        }

        foreach (var testCase in testCases)
        {
            await writeService.WriteTestCase(testCase);
        }

        var mainJson = new Root
        {
            ProjectName = project.Name,
            Sections = new List<Section> { sectionsInfo.MainSection },
            TestCases = testCases.Select(t => t.Id).ToList(),
            SharedSteps = sharedStepsInfo.SharedSteps.Select(s => s.Id).ToList(),
        };

        await writeService.WriteMainJson(mainJson);

        logger.LogInformation("Ending export");
    }
}
