using SpiraTestExporter.Models;

namespace SpiraTestExporter.Services;

public interface ITestCaseService
{
    Task<TestCaseData> ConvertTestCases(int projectId, Dictionary<int, Guid> sectionMap,
        Dictionary<int, string> priorities, Dictionary<int, string> statuses, Dictionary<string, Guid> attributesMap);
}
