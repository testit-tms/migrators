using Models;

namespace TestLinkExporter.Services;

public interface ITestCaseService
{
    Task<List<TestCase>> ConvertTestCases(Dictionary<int, Guid> sectionMap, Dictionary<string, Guid> attributes);
}
