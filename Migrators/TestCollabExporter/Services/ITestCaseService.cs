using Models;

namespace TestCollabExporter.Services;

public interface ITestCaseService
{
    Task<List<TestCase>> ConvertTestCases(int projectId, Dictionary<int, Guid> sectionMap,
        Dictionary<string, Guid> attributes, Dictionary<int, Guid> sharedStepsMap);
}
