using AllureExporter.Client;
using AllureExporter.Helpers;
using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;

namespace AllureExporter.Services.Implementations;

internal sealed class ExportService(
    ILogger<ExportService> logger,
    IClient client,
    IWriteService writeService,
    ISectionService sectionService,
    ISharedStepService sharedStepService,
    ITestCaseService testCaseService,
    ICoreHelper coreHelper,
    IAttributeService attributeService)
    : IExportService
{
    private const bool IsLongTagsExcludeEnabled = false;
    private const bool IsLongTagsCutEnabled = true;


    public async Task ExportProject()
    {
        logger.LogInformation("Starting export");

        var project = await client.GetProjectId();
        var section = await sectionService.ConvertSection(project.Id);
        var attributes = await attributeService.GetCustomAttributes(project.Id);

        var customAttributes = attributes.ToDictionary(k => k.Name, v => v.Id);
        var sharedSteps = await sharedStepService.ConvertSharedSteps(project.Id, section.MainSection.Id, attributes);
        var sharedStepsMap = sharedSteps.ToDictionary(k => k.Key.ToString(), v => v.Value.Id);
        var testCases =
            await testCaseService.ConvertTestCases(project.Id, sharedStepsMap, customAttributes, section);

        foreach (var sharedStep in sharedSteps)
        {
            if (IsLongTagsExcludeEnabled) coreHelper.ExcludeLongTags(sharedStep.Value);
            if (IsLongTagsCutEnabled) coreHelper.CutLongTags(sharedStep.Value);

            await writeService.WriteSharedStep(sharedStep.Value);
        }

        foreach (var testCase in testCases)
        {
            if (IsLongTagsExcludeEnabled) coreHelper.ExcludeLongTags(testCase);
            if (IsLongTagsCutEnabled) coreHelper.CutLongTags(testCase);

            await writeService.WriteTestCase(testCase);
        }

        var mainJson = new Root
        {
            ProjectName = project.Name,
            Sections = [section.MainSection],
            TestCases = testCases.Select(t => t.Id).ToList(),
            SharedSteps = sharedSteps.Values.Select(s => s.Id).ToList(),
            Attributes = attributes
        };

        await writeService.WriteMainJson(mainJson);

        logger.LogInformation("Ending export");
    }
}
