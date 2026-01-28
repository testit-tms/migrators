using Models;
using QaseExporter.Models;

namespace QaseExporter.Services;

public interface ITestCaseService
{
    Task<TestCaseData> ConvertTestCases(Dictionary<int, Guid> sectionMap, Dictionary<string, SharedStep> sharedSteps, AttributeData attributes);
}
