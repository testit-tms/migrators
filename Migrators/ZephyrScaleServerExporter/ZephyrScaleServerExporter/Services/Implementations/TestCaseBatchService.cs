using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models;
using ZephyrScaleServerExporter.Models.Common;
using ZephyrScaleServerExporter.Models.TestCases;
using ZephyrScaleServerExporter.Models.TestCases.Export;
using ZephyrScaleServerExporter.Services.TestCase;
using Attribute = Models.Attribute;

namespace ZephyrScaleServerExporter.Services.Implementations;

internal class TestCaseBatchService(
    IOptions<AppConfig> config,
    ITestCaseCommonService testCaseCommonService,
    ITestCaseErrorLogService testCaseErrorLogService,
    ILogger<TestCaseBatchService> logger,
    IClient client,
    IWriteService writeService)
    : ITestCaseBatchService
{
    private HashSet<string> _savedTestCaseNames = [];
    private string _batchProcessedFile = string.Empty;

    public async Task<TestCaseData> ExportTestCasesBatch(SectionData sectionData,
        Dictionary<string, Attribute> attributeMap, string projectId)
    {
        InitBatch();
        logger.LogInformation("Converting test cases");

        var prepDataModel = await testCaseCommonService
            .PrepareForTestCasesExportAsync(attributeMap, projectId);

        var testCaseIds = await ProcessBatchCycleBlock(
            sectionData, attributeMap, prepDataModel, testCaseCommonService.GetTestCasesByConfig);

        if (config.Value.Zephyr.ExportArchived)
        {
            var archivedTestCaseIds = await ProcessBatchCycleBlock(
                sectionData, attributeMap, prepDataModel, testCaseCommonService.GetArchivedTestCases);
            testCaseIds.AddRange(archivedTestCaseIds);
        }

        return testCaseCommonService
            .PrepareTestCaseIdsData(attributeMap, prepDataModel.OwnersAttribute, testCaseIds);
    }


    private void InitBatch()
    {
        _batchProcessedFile = $"{config.Value.Zephyr.ProjectKey}-batch.txt";
        CreateBatchIfNotExists(_batchProcessedFile);
        // read file strings to hashset
        _savedTestCaseNames = new HashSet<string>(File.ReadLines(_batchProcessedFile));
    }

    /// <summary>
    /// берем, фильтруем, процессим, сохраняем, идем дальше. На выходе - список всех айдишников
    /// </summary>
    /// <returns></returns>
    private async Task<List<Guid>> ProcessBatchCycleBlock(SectionData sectionData,
        Dictionary<string, Attribute> attributeMap,
        TestCaseExportRequiredModel requiredModel,
        Func<IOptions<AppConfig>, IClient, int, int, string, Task<List<ZephyrTestCase>>> getTestCases)
    {
        List<Guid> testCaseIds = [];

        var startAt = 0;
        var retryCount = 0;
        var maxResults = 100;
        int countOfTests = 0;

        var maxCountPerBatch = config.Value.Zephyr.Count;
        if (maxCountPerBatch < maxResults)
        {
            maxResults = maxCountPerBatch;
        }

        // until there is new data
        while (true)
        {
            (var origCases, retryCount) = await TryGetTestCasesWithRetries(
                requiredModel, retryCount, startAt, maxResults, getTestCases);

            if (origCases == null)
            {
                // Пропускаем этот пакет и пытаемся получить следующий
                // Переходим к следующей итерации цикла
                continue;
            }

            if (origCases.Count == 0)
            {
                OnZeroOriginalCasesGreetEndForBatch(countOfTests);
                break;
            }

            var cases = FilterCasesByProcessed(origCases);
            // skip block if filtered everything
            if (cases.Count == 0)
            {
                startAt += maxResults;
                continue;
            }

            (startAt, countOfTests, var isBreak) = await WriteFiltered(cases, sectionData,
                attributeMap, requiredModel, testCaseIds,
                startAt, countOfTests, maxResults, maxCountPerBatch);

            if (isBreak)
                break;
        }

        return testCaseIds;
    }


    private async Task<(List<ZephyrTestCase>?, int)> TryGetTestCasesWithRetries(
        TestCaseExportRequiredModel requiredModel,
        int retryCount, int startAt, int maxResults,
        Func<IOptions<AppConfig>, IClient, int, int, string, Task<List<ZephyrTestCase>>> getTestCases)
    {
        const int maxRetries = 5; // Максимальное количество попыток
        try
        {
            var origCases = await getTestCases(config, client,
                startAt, maxResults,
                requiredModel.StatusData.StringStatuses);
            retryCount = 0; // Сбрасываем счетчик при успехе
            return (origCases, retryCount);
        }
        catch (Exception ex)
        {
            retryCount++;
            logger.LogError(ex, "Failed to get test cases starting from {StartAt} in batch mode (Attempt {RetryCount}/{MaxRetries})", startAt, retryCount, maxRetries);

            if (retryCount >= maxRetries)
            {
                logger.LogCritical("Failed to get test cases after {MaxRetries} attempts in batch mode. Exiting.", maxRetries);
                Console.WriteLine("Press ENTER to exit...");
                Console.ReadLine();
                Environment.Exit(1); // Выход из приложения
            }

            logger.LogWarning("Waiting 3 seconds before next attempt in batch mode...");
            await Task.Delay(3000); // Задержка 3 секунды
        }
        // Пропускаем этот пакет и пытаемся получить следующий
        // Переходим к следующей итерации цикла
        return (null, retryCount);
    }

    private void OnZeroOriginalCasesGreetEndForBatch(int countOfTests)
    {
        if (countOfTests > 0)
        {
            // save main.json
            GreetEndOfBatchProcessing(countOfTests);
            return;
        }

        // exit without saving main.json
        GreetNoDataForCurrentBatch();
        Console.WriteLine("Press ENTER to exit...");
        Console.ReadLine();
        Environment.Exit(1);
    }

    private async Task<(int, int, bool)> WriteFiltered(
        List<ZephyrTestCase> cases,
        SectionData sectionData,
        Dictionary<string, Attribute> attributeMap,
        TestCaseExportRequiredModel requiredModel,
        List<Guid> testCaseIds,
        int startAt,
        int countOfTests,
        int maxResults,
        int maxCountPerBatch
        )
    {
        var isBreak = false;
        try
        {
            var ids = await testCaseCommonService.WriteTestCasesAsync(
                cases,
                sectionData,
                attributeMap,
                requiredModel.RequiredAttributeNames,
                requiredModel.OwnersAttribute);

            testCaseIds.AddRange(ids);

            startAt += maxResults;
            countOfTests += cases.Count;

            logger.LogInformation("Got {GetCount} test cases and wrote {WriteCount}", countOfTests,
                testCaseIds.Count);

            // batch logic:
            AddSavedCases(_batchProcessedFile, cases);
            if (countOfTests >= maxCountPerBatch)
            {
                logger.LogInformation("Batch finished with {CountOfTests} test cases", countOfTests);
                isBreak = true;
                return (startAt, countOfTests, isBreak);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during TestCaseBatchService tc postprocessing");
            testCaseErrorLogService.LogError(ex, "An error occurred during TestCaseBatchService tc postprocessing", null, cases);
            throw;
        }

        // false for now
        return (startAt, countOfTests, isBreak);
    }

    private static void CreateBatchIfNotExists(string path)
    {
        if (!File.Exists(path))
        {
            File.Create(path).Close();
        }
    }

    private List<ZephyrTestCase> FilterCasesByProcessed(List<ZephyrTestCase> testCases)
    {
        return testCases.Where(x => !_savedTestCaseNames.Contains(x.Name)).ToList();
    }

    private static void AddSavedCases(string path, List<ZephyrTestCase> testCases)
    {
        File.AppendAllLines(path, testCases.Select(x => x.Name));
    }

    private void GreetEndOfBatchProcessing(int countOfTests)
    {
        logger.LogInformation("[SUCCESS] This is the last batch, no more data for export exists");
        logger.LogInformation("Last export: {CountOfTests} test cases, total batches: {Number}",
            countOfTests, writeService.GetBatchNumber());
        logger.LogInformation(
            "You can check {BatchProcessedFile} for all test cases names exported with all batches (one per line)",
            _batchProcessedFile);
    }

    private void GreetNoDataForCurrentBatch()
    {
        logger.LogInformation("[SUCCESS] This is the last batch, no more data for export exists");
        logger.LogInformation("total batches: {Number}", writeService.GetBatchNumber() - 1);
        logger.LogInformation(
            "You can check {BatchProcessedFile} for all test cases names exported with all batches (one per line)",
            _batchProcessedFile);

    }







}
