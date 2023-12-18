using HPALMExporter.Models;

namespace HPALMExporter.Services;

public interface ITestCaseService
{
    Task<TestCaseData> ConvertTestCases(Dictionary<int, Guid> sectionMap,
        Dictionary<string, Guid> attributeMap);
}
