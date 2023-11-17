using PractiTestExporter.Models;

namespace PractiTestExporter.Services;

public interface ITestCaseService
{
    Task<TestCaseData> ConvertTestCases(Guid sectionId, Dictionary<string, Guid> attributeMap);
}
