using Attribute = Models.Attribute;

namespace ZephyrScaleServerExporter.Models.Attributes;

public class AttributeData
{
    public required List<Attribute> Attributes { get; set; }
    public required Dictionary<string, Attribute> AttributeMap { get; set; }

    public Dictionary<int, string>? StateMap { get; set; }

    public Dictionary<int, string>? PriorityMap { get; set; }
}
