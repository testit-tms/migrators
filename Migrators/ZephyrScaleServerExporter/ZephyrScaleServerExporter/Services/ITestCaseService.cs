using ZephyrScaleServerExporter.Models.Common;
using ZephyrScaleServerExporter.Models.TestCases;
using Attribute = Models.Attribute;

namespace ZephyrScaleServerExporter.Services;

public interface ITestCaseService
{
    Task<TestCaseData> ExportTestCases(SectionData sectionData, Dictionary<string, Attribute> attributeMap, string projectId);
    

}
