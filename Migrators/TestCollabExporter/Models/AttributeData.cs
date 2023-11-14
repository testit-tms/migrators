using Attribute = Models.Attribute;

namespace TestCollabExporter.Models;

public class AttributeData
{
    public List<Attribute> Attributes { get; set; }
    public Dictionary<string, Guid> AttributesMap { get; set; }
}
