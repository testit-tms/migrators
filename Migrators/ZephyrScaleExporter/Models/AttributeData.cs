using Attribute = Models.Attribute;

namespace ZephyrScaleExporter.Models;

public class AttributeData
{
    public List<Attribute> Attributes { get; set; }

    public Dictionary<string, Guid> AttributeMap { get; set; }

    public Dictionary<int, string> StateMap { get; set; }

    public Dictionary<int, string> PriorityMap { get; set; }
}
