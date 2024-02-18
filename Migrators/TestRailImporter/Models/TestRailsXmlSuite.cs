using System.Xml.Serialization;

namespace TestRailImporter.Models;

[XmlRoot(ElementName = "suite")]
public record TestRailsXmlSuite
{
    [XmlElement(ElementName = "name")]
    public string? Name { get; set; }

    [XmlArray(ElementName = "sections")]
    [XmlArrayItem("section", Type = typeof(TestRailsXmlSection))]
    public TestRailsXmlSection[]? Sections { get; set; }
}
