using Models;

namespace AzureExporter.Services;

public interface ITestCaseService
{
    Task<List<TestCase>> ConvertTestCases(Guid projectId, Dictionary<int, Guid> sharedStepMap, Guid sectionId, Dictionary<string, Guid> attributeMap);
}
