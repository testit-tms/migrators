using Models;
using Attribute = Models.Attribute;

namespace ZephyrScaleServerExporter.Models;

public class TestCaseData
{
    public List<TestCase> TestCases { get; set; }
    public List<Attribute> Attributes { get; set; }
}
