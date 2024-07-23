using Models;
using ZephyrScaleServerExporter.Models;
using Attribute = Models.Attribute;

namespace ZephyrScaleServerExporter.Services;

public interface ITestCaseService
{
    Task<List<TestCase>> ConvertTestCases(SectionData sectionData, Dictionary<string, Attribute> attributeMap);
}
