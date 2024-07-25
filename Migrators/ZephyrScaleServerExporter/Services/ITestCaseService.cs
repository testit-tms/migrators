using Models;
using ZephyrScaleServerExporter.Models;
using Attribute = Models.Attribute;

namespace ZephyrScaleServerExporter.Services;

public interface ITestCaseService
{
    Task<TestCaseData> ConvertTestCases(SectionData sectionData, Dictionary<string, Attribute> attributeMap);
}
