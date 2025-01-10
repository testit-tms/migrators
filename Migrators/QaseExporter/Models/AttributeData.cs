using Attribute = Models.Attribute;

namespace QaseExporter.Models;

public class AttributeData
{
    public List<Attribute> Attributes { get; set; } = new();

    public Dictionary<QaseCustomField, Guid> CustomAttributeMap { get; set; } = new();

    public Dictionary<QaseSystemField, Guid> SystemAttributeMap { get; set; } = new();

    public Dictionary<string, Attribute> AttributeMap { get; set; } = new();
}
