using QaseExporter.Models;

namespace QaseExporter.Services;

public interface ITestResultService
{
    Task<TestResultData> ConvertTestResults(string testRunHash, Dictionary<int, Guid> testCaseMap);
}
