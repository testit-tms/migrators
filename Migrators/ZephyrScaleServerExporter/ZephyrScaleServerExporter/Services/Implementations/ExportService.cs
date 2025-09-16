using Microsoft.Extensions.Logging;
using Models;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models.Attributes;
using ZephyrScaleServerExporter.Models.Client;
using ZephyrScaleServerExporter.Models.Common;
using ZephyrScaleServerExporter.Models.TestCases;

namespace ZephyrScaleServerExporter.Services.Implementations;

internal class ExportService(
    ILogger<ExportService> logger,
    IClient client,
    IFolderService folderService,
    IAttributeService attributeService,
    ITestCaseService testCaseService,
    ITestCaseBatchService testCaseBatchService,
    IWriteService writeService)
    : IExportService
{
    public async Task ExportProject()
    {
        logger.LogInformation("Exporting project");
        var (project, folders, attributes) = await FetchProjectData();

        var testCaseData = await testCaseService.ExportTestCases(folders, attributes.AttributeMap, project.Id);

        await WriteMainRoot(project, folders, testCaseData);
    }

    public async Task ExportProjectCloud()
    {
        logger.LogInformation("Exporting project");
        var (project, folders, attributes) = await FetchProjectDataCloud();

        var testCaseData = await testCaseService.ExportTestCasesCloud(folders, attributes.AttributeMap,
            project.Id, project.Key);

        await WriteMainRoot(project, folders, testCaseData);
    }

    // записали в батч файл все сохраненные тест-кейсы
    // можно создавать второй файл main со всеми линками без привязки к первому
    public async Task ExportProjectBatch()
    {
        logger.LogInformation("Exporting project batch");
        var (project, folders, attributes) = await FetchProjectData();

        var testCaseData = await testCaseBatchService.ExportTestCasesBatch(folders, attributes.AttributeMap, project.Id);

        await WriteMainRoot(project, folders, testCaseData);
    }

    private async Task<(ZephyrProject, SectionData, AttributeData)> FetchProjectData()
    {
        var project = await client.GetProject();
        var folders = folderService.ConvertSections(project.Name);
        var attributes = await attributeService.ConvertAttributes(project.Id);
        return (project, folders, attributes);
    }

    private async Task<(ZephyrProject, SectionData, AttributeData)> FetchProjectDataCloud()
    {
        var project = await client.GetProjectCloud();
        var folders = folderService.ConvertSections(project.Name);
        var attributes = await attributeService.ConvertAttributesCloud(project.Key);
        return (project, folders, attributes);
    }

    private async Task WriteMainRoot(ZephyrProject project,
        SectionData folders,
        TestCaseData testCaseData
    )
    {
        var root = new Root
        {
            ProjectName = project.Name,
            Attributes = testCaseData.Attributes,
            Sections = [folders.MainSection],
            SharedSteps = [],
            TestCases = testCaseData.TestCaseIds
        };

        await writeService.WriteMainJson(root);

        logger.LogInformation("Export complete");
    }
}
