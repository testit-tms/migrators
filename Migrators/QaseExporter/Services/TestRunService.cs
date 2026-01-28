using Microsoft.Extensions.Logging;
using Models;
using QaseExporter.Client;
using QaseExporter.Models;

namespace QaseExporter.Services;

public class TestRunService : ITestRunService
{
    private readonly ILogger<TestCaseService> _logger;
    private readonly IClient _client;
    private readonly ITestResultService _testResultService;
    private readonly Dictionary<string, TestPlan> _testPlanMap;

    public TestRunService(ILogger<TestCaseService> logger, IClient client, ITestResultService testResultService)
    {
        _logger = logger;
        _client = client;
        _testResultService = testResultService;
        _testPlanMap = new Dictionary<string, TestPlan>();
    }

    public async Task<TestRunData> ConvertTestRuns(Dictionary<int, Guid> testCaseMap)
    {
        _logger.LogInformation("Converting test runs");

        var allTestRuns = new List<TestRun>();

        var qaseTestRuns = await _client.GetTestRuns();

        _logger.LogDebug("Found {Count} test runs", qaseTestRuns.Count);

        foreach (var qaseTestRun in qaseTestRuns)
        {
            var testResultData = await GetTestResultData(qaseTestRun.Id, testCaseMap);

            var testRun = ConvertTestRun(qaseTestRun, testResultData.AutoTestResults);

            if (testRun != null)
            {
                allTestRuns.Add(testRun);
            }

            if (!string.IsNullOrEmpty(qaseTestRun.PlanId))
            {
                await ConvertExistedTestPlan(qaseTestRun, testResultData.ManualTestResults, testRun);
            }
            else
            {
                ConvertTestPlanByRun(qaseTestRun, testResultData.ManualTestResults, testRun);
            }
        }

        _logger.LogDebug("Exported {RunCount} test runs and {PlanCount} test plans",
            allTestRuns.Count, _testPlanMap.Keys.Count);

        return new()
        {
            TestRuns = allTestRuns,
            TestPlans = _testPlanMap.Values.ToList(),
        };
    }

    private async Task<TestResultData> GetTestResultData(int qaseTestRunId, Dictionary<int, Guid> testCaseMap)
    {
        var qaseTestRunHash = await _client.GetTestRunHash(qaseTestRunId);

        if (string.IsNullOrEmpty(qaseTestRunHash))
        {
            return new();
        }

        var testResultData = await _testResultService.ConvertTestResults(qaseTestRunHash, testCaseMap);

        return testResultData;
    }

    private TestRun? ConvertTestRun(QaseTestRun qaseTestRun, List<TestResult> autoTestResults)
    {
        if (autoTestResults.Count == 0)
        {
            return null;
        }

        return new()
        {
            Id = Guid.NewGuid(),
            Name = qaseTestRun.Title,
            Description = qaseTestRun.Description,
            AutoTestResultIds = autoTestResults.Select(r => r.Id).ToList(),
        };
    }

    private void ConvertTestPlanByRun(QaseTestRun qaseTestRun, List<TestResult> manualTestResults, TestRun? testRun)
    {
        if (manualTestResults.Count == 0)
        {
            return;
        }

        var testPlan = new TestPlan()
        {
            Id = Guid.NewGuid(),
            Name = qaseTestRun.Title,
            Description = qaseTestRun.Description,
            TestRunIds = testRun != null ? [testRun.Id] : new(),
            ManualTestResultIds = manualTestResults.Select(r => r.Id).ToList(),
        };

        _testPlanMap.Add(testPlan.Id.ToString(), testPlan);
    }

    private async Task ConvertExistedTestPlan(QaseTestRun qaseTestRun, List<TestResult> manualTestResults, TestRun? testRun)
    {
        if (_testPlanMap.TryGetValue(qaseTestRun.PlanId, out var plan))
        {
            if (testRun != null)
            {
                plan.TestRunIds.Add(testRun.Id);
            }

            return;
        }

        var qaseTestPlan = await _client.GetTestPlan(qaseTestRun.PlanId);

        var testPlan = new TestPlan()
        {
            Id = Guid.NewGuid(),
            Name = qaseTestPlan.Title,
            Description = qaseTestPlan.Description,
            TestRunIds = testRun != null ? [testRun.Id] : new(),
            ManualTestResultIds = manualTestResults.Select(r => r.Id).ToList(),
        };

        _testPlanMap.Add(qaseTestPlan.Id.ToString(), testPlan);
    }
}
