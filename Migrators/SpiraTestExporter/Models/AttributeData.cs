using Attribute = Models.Attribute;

namespace SpiraTestExporter.Models;

public class AttributeData
{
    public List<Attribute> Attributes { get; set; }
    public Dictionary<string, Guid> AttributesMap { get; set; }
    public Dictionary<int, string> PrioritiesMap { get; set; }
    public Dictionary<int, string> StatusesMap { get; set; }
}
