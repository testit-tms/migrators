using ZephyrScaleServerExporter.Models;

namespace ZephyrScaleServerExporter.Services;

public interface ITestCaseService
{
    Task<TestCaseData> ConvertTestCases(SectionData sectionData, Dictionary<string, Guid> attributeMap);
}
