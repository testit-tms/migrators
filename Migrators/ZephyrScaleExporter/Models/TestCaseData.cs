using Models;
using Attribute = Models.Attribute;

namespace ZephyrScaleExporter.Models;

public class TestCaseData
{
    public List<TestCase> TestCases { get; set; }
    public List<Attribute> Attributes { get; set; }
}
