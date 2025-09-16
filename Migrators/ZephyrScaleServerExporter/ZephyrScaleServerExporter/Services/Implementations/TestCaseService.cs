using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models;
using ZephyrScaleServerExporter.Models.Client;
using ZephyrScaleServerExporter.Models.Common;
using ZephyrScaleServerExporter.Models.TestCases;
using ZephyrScaleServerExporter.Models.TestCases.Export;
using ZephyrScaleServerExporter.Services.TestCase;
using Attribute = Models.Attribute;

namespace ZephyrScaleServerExporter.Services.Implementations;

internal class TestCaseService(
    IOptions<AppConfig> config,
    ITestCaseCommonService testCaseCommonService,
    ILogger<TestCaseService> logger,
    IClient client)
    : ITestCaseService
{
    public async Task<TestCaseData> ExportTestCases(SectionData sectionData,
        Dictionary<string, Attribute> attributeMap, string projectId)
    {
        logger.LogInformation("Converting test cases");

        var prepDataModel = await testCaseCommonService
            .PrepareForTestCasesExportAsync(attributeMap, projectId);

        var testCaseIds = await ProcessCycleBlock(
            sectionData, attributeMap, prepDataModel, testCaseCommonService.GetTestCasesByConfig);

        if (config.Value.Zephyr.ExportArchived)
        {
            var archivedTestCaseIds = await ProcessCycleBlock(
                sectionData, attributeMap, prepDataModel, testCaseCommonService.GetArchivedTestCases);
            testCaseIds.AddRange(archivedTestCaseIds);
        }

        return testCaseCommonService
            .PrepareTestCaseIdsData(attributeMap, prepDataModel.OwnersAttribute, testCaseIds);
    }

    public async Task<TestCaseData> ExportTestCasesCloud(SectionData sectionData,
        Dictionary<string, Attribute> attributeMap, string projectId, string projectKey)
    {
        logger.LogInformation("Converting test cases");

        var prepDataModel = await testCaseCommonService
            .CloudPrepareForTestCasesExportAsync(attributeMap, projectId, projectKey);

        var testCaseIds = await ProcessCycleBlockCloud(
            sectionData, attributeMap, prepDataModel, testCaseCommonService.GetTestCasesByConfigCloud);

        // TODO: archived export not implemented yet
        // if (config.Value.Zephyr.ExportArchived)
        // {
        //     var archivedTestCaseIds = await ProcessCycleBlock(
        //         sectionData, attributeMap, prepDataModel, testCaseCommonService.GetArchivedTestCases);
        //     testCaseIds.AddRange(archivedTestCaseIds);
        // }

        return testCaseCommonService
            .PrepareTestCaseIdsData(attributeMap, prepDataModel.OwnersAttribute, testCaseIds);
    }

    private async Task<List<Guid>> ProcessCycleBlock(SectionData sectionData,
        Dictionary<string, Attribute> attributeMap,
        TestCaseExportRequiredModel requiredModel,
        Func<IOptions<AppConfig>, IClient, int, int, string, Task<List<ZephyrTestCase>>> getTestCasesFunc)
    {
        List<Guid> testCaseIds = [];

        var startAt = 0;
        var maxResults = 100;
        var countOfTests = 0;
        var retryCount = 0; // Счетчик последовательных ошибок
        const int maxRetries = 5; // Максимальное количество попыток

        // until there is new data
        while (true)
        {
            List<ZephyrTestCase> origCases;
            try
            {
                origCases = await getTestCasesFunc(config, client,
                    startAt, maxResults,
                    requiredModel.StatusData.StringStatuses);
                retryCount = 0; // Сбрасываем счетчик при успехе
            }
            catch (Exception ex)
            {
                retryCount++;
                logger.LogError(ex, "Failed to get test cases starting from {StartAt} (Attempt {RetryCount}/{MaxRetries})", startAt, retryCount, maxRetries);

                if (retryCount >= maxRetries)
                {
                    logger.LogCritical("Failed to get test cases after {MaxRetries} attempts. Exiting.", maxRetries);
                    Console.WriteLine("Press ENTER to exit...");
                    Console.ReadLine();
                    Environment.Exit(1); // Выход из приложения
                }

                logger.LogWarning("Waiting 3 seconds before next attempt...");
                await Task.Delay(3000); // Задержка 3 секунды

                // Пропускаем этот пакет и пытаемся получить следующий
                continue; // Переходим к следующей итерации цикла
            }

            if (origCases.Count == 0)
            {
                break;
            }

            var ids = await testCaseCommonService.WriteTestCasesAsync(
                origCases,
                sectionData,
                attributeMap,
                requiredModel.RequiredAttributeNames,
                requiredModel.OwnersAttribute);

            testCaseIds.AddRange(ids);

            startAt += maxResults;
            countOfTests += origCases.Count;

            logger.LogInformation("Got {GetCount} test cases and wrote {WriteCount}", countOfTests, testCaseIds.Count);
        }

        return testCaseIds;
    }


    private async Task<List<Guid>> ProcessCycleBlockCloud(SectionData sectionData,
        Dictionary<string, Attribute> attributeMap,
        TestCaseExportRequiredModel requiredModel,
        Func<IOptions<AppConfig>, IClient, int, int, string, Task<List<CloudZephyrTestCase>>> getTestCasesFunc)
    {
        List<Guid> testCaseIds = [];

        var startAt = 0;
        var maxResults = 100;
        var countOfTests = 0;
        var retryCount = 0; // Счетчик последовательных ошибок
        const int maxRetries = 5; // Максимальное количество попыток

        // until there is new data
        while (true)
        {
            List<CloudZephyrTestCase> origCases;
            try
            {
                origCases = await getTestCasesFunc(config, client,
                    startAt, maxResults,
                    requiredModel.StatusData.StringStatuses);
                retryCount = 0; // Сбрасываем счетчик при успехе
            }
            catch (Exception ex)
            {
                retryCount++;
                logger.LogError(ex, "Failed to get test cases starting from {StartAt} (Attempt {RetryCount}/{MaxRetries})", startAt, retryCount, maxRetries);

                if (retryCount >= maxRetries)
                {
                    logger.LogCritical("Failed to get test cases after {MaxRetries} attempts. Exiting.", maxRetries);
                    Console.WriteLine("Press ENTER to exit...");
                    Console.ReadLine();
                    Environment.Exit(1); // Выход из приложения
                }

                logger.LogWarning("Waiting 3 seconds before next attempt...");
                await Task.Delay(3000); // Задержка 3 секунды

                // Пропускаем этот пакет и пытаемся получить следующий
                continue; // Переходим к следующей итерации цикла
            }

            if (origCases.Count == 0)
            {
                break;
            }

            var ids = await testCaseCommonService.WriteTestCasesCloudAsync(
                origCases,
                sectionData,
                attributeMap,
                requiredModel.RequiredAttributeNames,
                requiredModel.OwnersAttribute);

            testCaseIds.AddRange(ids);

            startAt += maxResults;
            countOfTests += origCases.Count;

            logger.LogInformation("Got {GetCount} test cases and wrote {WriteCount}", countOfTests, testCaseIds.Count);
        }

        return testCaseIds;
    }

}
