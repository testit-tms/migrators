using Models;
using QaseExporter.Models;
using Attribute = Models.Attribute;

namespace QaseExporter.Services;

public interface ITestCaseService
{
    Task<List<TestCase>> ConvertTestCases(Dictionary<int, Guid> sectionMap, Dictionary<string, SharedStep> sharedSteps, AttributeData attributes);
}
