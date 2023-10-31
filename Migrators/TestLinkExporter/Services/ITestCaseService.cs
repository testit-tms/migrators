using Models;

namespace TestLinkExporter.Services;

public interface ITestCaseService
{
    List<TestCase> ConvertTestCases(Dictionary<int, Guid> sectionMap);
}
