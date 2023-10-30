using XRayExporter.Models;

namespace XRayExporter.Services;

public interface ITestCaseService
{
    Task<TestCaseData> ConvertTestCases(Dictionary<int, Guid> sectionMap);
}
