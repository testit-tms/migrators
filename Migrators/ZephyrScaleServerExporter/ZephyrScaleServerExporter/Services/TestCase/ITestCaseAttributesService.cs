using Models;
using ZephyrScaleServerExporter.Models.TestCases;
using Attribute = Models.Attribute;

namespace ZephyrScaleServerExporter.Services.TestCase;

public interface ITestCaseAttributesService
{
    List<CaseAttribute> CalculateAttributes(ZephyrTestCase zephyrTestCase,
        Dictionary<string, Attribute> attributeMap,
        List<string> requiredAttributeNames);
}