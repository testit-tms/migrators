using Microsoft.Extensions.Logging;
using Models;
using QaseExporter.Client;
using QaseExporter.Models;

namespace QaseExporter.Services;

public class TestResultService : ITestResultService
{
    private readonly ILogger<TestCaseService> _logger;
    private readonly IClient _client;

    public TestResultService(ILogger<TestCaseService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<TestResultData> ConvertTestResults(string testRunHash, Dictionary<int, Guid> testCaseMap)
    {
        _logger.LogInformation("Converting test results");

        var testResultData = new TestResultData();

        var qaseTestResultStatMap = await _client.GetTestResultStats(testRunHash);

        foreach (var qaseTestResultHashAndStat in qaseTestResultStatMap)
        {
            try
            {
                var status = ConvertStatus(qaseTestResultHashAndStat.Value.Status);
                var qaseTestResult = await _client.GetTestResult(testRunHash, qaseTestResultHashAndStat.Key);

                if (qaseTestResult == null)
                {
                    continue;
                }

                var testResult = ConvertTestResult(
                    qaseTestResult,
                    testCaseMap,
                    status,
                    qaseTestResultHashAndStat.Value.Time
                );

                if (qaseTestResult.Automated)
                {
                    testResultData.AutoTestResults.Add(testResult);

                    continue;
                }

                testResultData.ManualTestResults.Add(testResult);
            }
            catch
            {
                _logger.LogWarning("Can't convert test result with status {Status}. Skip test result",
                    qaseTestResultHashAndStat.Value.Status);
            }
        }

        _logger.LogDebug("Exported {ManualCount} manual test results and {AutoCount} auto test results for test run {Hash}",
            testResultData.ManualTestResults.Count, testResultData.AutoTestResults.Count, testRunHash);

        return testResultData;
    }

    private static TestResult ConvertTestResult(
        QaseTestResult qaseTestResult,
        Dictionary<int, Guid> testCaseMap,
        Outcome status,
        int duration)
    {
        return new TestResult
        {
            Id = Guid.NewGuid(),
            TestCaseId = testCaseMap[qaseTestResult.CaseId],
            StatusCode = status,
            Duration = duration,
        };
    }

    private static Outcome ConvertStatus(int status)
    {
        return status switch
        {
            1 => Outcome.Passed,
            2 => Outcome.Failed,
            3 => Outcome.Blocked,
            5 => Outcome.Skipped,
            8 => Outcome.Blocked,
            _ => throw new ArgumentException($"Can't convert status {status}")
        };
    }
}
