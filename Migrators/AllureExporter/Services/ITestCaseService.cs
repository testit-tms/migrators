using Models;

namespace AllureExporter.Services;

public interface ITestCaseService
{
    Task<List<TestCase>> ConvertTestCase(int projectId, Guid statusAttribute, Dictionary<int, Guid> sectionIdMap);
}
