using Attribute = Models.Attribute;

namespace QaseExporter.Models;

public class AttributeData
{
    public List<Attribute> Attributes { get; set; }

    public Dictionary<QaseCustomField, Guid> CustomAttributeMap { get; set; }

    public Dictionary<QaseSystemField, Guid> SystemAttributeMap { get; set; }

    public Dictionary<string, Attribute> AttributeMap { get; set; }
}
