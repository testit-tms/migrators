using ZephyrScaleServerExporter.Models.Common;
using ZephyrScaleServerExporter.Models.TestCases;
using Attribute = Models.Attribute;

namespace ZephyrScaleServerExporter.Services;

public interface ITestCaseBatchService
{
    Task<TestCaseData> ExportTestCasesBatch(SectionData sectionData, Dictionary<string, Attribute> attributeMap, string projectId);
}