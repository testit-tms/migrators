using ZephyrScaleExporter.Models;

namespace ZephyrScaleExporter.Services;

public interface ITestCaseService
{
    Task<TestCaseData> ConvertTestCases(Dictionary<int, Guid> sectionMap, Dictionary<string, Guid> attributeMap,
        Dictionary<int, string> statusMap, Dictionary<int, string> priorityMap);
}
