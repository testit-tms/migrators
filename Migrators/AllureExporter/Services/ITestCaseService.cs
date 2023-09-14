using Models;

namespace AllureExporter.Services;

public interface ITestCaseService
{
    Task<List<TestCase>> ConvertTestCases(int projectId, Guid statusAttribute, Dictionary<int, Guid> sectionIdMap);
}
