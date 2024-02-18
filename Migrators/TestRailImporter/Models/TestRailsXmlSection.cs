using System.Xml.Serialization;

namespace TestRailImporter.Models;

[XmlRoot(ElementName = "section")]
public record TestRailsXmlSection
{
    [XmlElement(ElementName = "name")]
    public string? Name { get; set; }

    [XmlArray(ElementName = "sections")]
    [XmlArrayItem("section", Type = typeof(TestRailsXmlSection))]
    public TestRailsXmlSection[]? Sections { get; set; }

    [XmlArray(ElementName = "cases")]
    [XmlArrayItem("case", Type = typeof(TestRailsXmlCase))]
    public TestRailsXmlCase[]? Cases { get; set; }

    [XmlElement(ElementName = "description")]
    public string? Description { get; set; }
}
