using ZephyrScaleServerExporter.Models.Client;
using ZephyrScaleServerExporter.Models.Common;
using ZephyrScaleServerExporter.Models.TestCases;
using Attribute = Models.Attribute;

namespace ZephyrScaleServerExporter.Services.TestCase;

public interface ITestCaseConvertService
{
    Task<global::Models.TestCase?> ConvertTestCase(
        ZephyrTestCase zephyrTestCase,
        SectionData sectionData,
        Dictionary<string, Attribute> attributeMap,
        List<string> requiredAttributeNames,
        Attribute ownersAttribute);

    Task<global::Models.TestCase?> ConvertTestCaseCloud(
        CloudZephyrTestCase zephyrTestCase,
        SectionData sectionData,
        Dictionary<string, Attribute> attributeMap,
        List<string> requiredAttributeNames,
        Attribute ownersAttribute);
}
