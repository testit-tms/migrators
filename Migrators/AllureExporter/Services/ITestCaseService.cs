using AllureExporter.Models;
using Models;

namespace AllureExporter.Services;

public interface ITestCaseService
{
    Task<List<TestCase>> ConvertTestCases(long projectId, Dictionary<string, Guid> sharedStepMap, Dictionary<string, Guid> attributes,  SectionInfo sectionInfo);
}
