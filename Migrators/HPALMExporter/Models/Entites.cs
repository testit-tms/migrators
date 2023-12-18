using System.Xml.Serialization;

namespace HPALMExporter.Models
{
    [XmlRoot(ElementName = "ChildrenCount")]
    public class ChildrenCount
    {
        [XmlElement(ElementName = "Value")] public string Value { get; set; }
    }

    [XmlRoot(ElementName = "Field")]
    public class Field
    {
        [XmlElement(ElementName = "Value")] public string Value { get; set; }
        [XmlAttribute(AttributeName = "Name")] public string Name { get; set; }
    }

    [XmlRoot(ElementName = "Fields")]
    public class Fields
    {
        [XmlElement(ElementName = "Field")] public List<Field> Field { get; set; }
    }

    [XmlRoot(ElementName = "Entity")]
    public class Entity
    {
        [XmlElement(ElementName = "ChildrenCount")]
        public ChildrenCount ChildrenCount { get; set; }

        [XmlElement(ElementName = "Fields")] public Fields Fields { get; set; }

        [XmlElement(ElementName = "RelatedEntities")]
        public string RelatedEntities { get; set; }

        [XmlAttribute(AttributeName = "Type")] public string Type { get; set; }
    }

    [XmlRoot(ElementName = "Entities")]
    public class Entities
    {
        [XmlElement(ElementName = "Entity")] public List<Entity> Entity { get; set; }

        [XmlElement(ElementName = "singleElementCollection")]
        public string SingleElementCollection { get; set; }

        [XmlAttribute(AttributeName = "TotalResults")]
        public int TotalResults { get; set; }
    }
}
