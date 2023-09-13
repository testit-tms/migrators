using Models;

namespace AllureExporter.Services;

public interface ITestCaseService
{
    Task<List<TestCase>> ConvertTestCases(int projectId, Guid statusAttribute, Guid layerAttribute, Dictionary<int, Guid> sectionIdMap);
}
