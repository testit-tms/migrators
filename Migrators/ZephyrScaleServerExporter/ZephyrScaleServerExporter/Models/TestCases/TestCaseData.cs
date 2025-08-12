using Attribute = Models.Attribute;

namespace ZephyrScaleServerExporter.Models.TestCases;

public class TestCaseData
{
    public required List<Guid> TestCaseIds { get; set; }
    public required List<Attribute> Attributes { get; set; }
}
