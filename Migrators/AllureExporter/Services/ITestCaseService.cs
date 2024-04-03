using Models;

namespace AllureExporter.Services;

public interface ITestCaseService
{
    Task<List<TestCase>> ConvertTestCases(int projectId, Dictionary<string, Guid> sharedStepMap, Dictionary<string, Guid> attributes, Dictionary<int, Guid> sectionIdMap);
}
