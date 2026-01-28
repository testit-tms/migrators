using QaseExporter.Models;

namespace QaseExporter.Services;

public interface ITestRunService
{
    Task<TestRunData> ConvertTestRuns(Dictionary<int, Guid> testCaseMap);
}
