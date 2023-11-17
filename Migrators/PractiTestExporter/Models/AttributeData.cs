using Attribute = Models.Attribute;

namespace PractiTestExporter.Models;

public class AttributeData
{
    public List<Attribute> Attributes { get; set; }
    public Dictionary<string, Guid> AttributeMap { get; set; }
}
