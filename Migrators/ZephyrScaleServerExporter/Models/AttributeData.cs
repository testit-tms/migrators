using Attribute = Models.Attribute;

namespace ZephyrScaleServerExporter.Models;

public class AttributeData
{
    public List<Attribute> Attributes { get; set; }

    public Dictionary<string, Attribute> AttributeMap { get; set; }
}
