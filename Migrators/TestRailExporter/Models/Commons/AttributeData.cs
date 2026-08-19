using TestRailExporter.Models.Client;
using Attribute = Models.Attribute;

namespace TestRailExporter.Models.Commons;

public class AttributeData
{
    public List<Attribute> Attributes { get; set; } = [];
    public Dictionary<string, Attribute> AttributesBySystemName { get; set; } = [];
    public Dictionary<string, TestRailCaseField> FieldsBySystemName { get; set; } = [];
    public Dictionary<int, string> PriorityNames { get; set; } = [];
    public Dictionary<int, string> StatusNames { get; set; } = [];
}
