using Models;
using Attribute = Models.Attribute;

namespace QaseExporter.Models;

public class TestCaseData
{
    public List<TestCase> TestCases { get; set; } = new();
    public List<Attribute> Attributes { get; set; } = new();
}
