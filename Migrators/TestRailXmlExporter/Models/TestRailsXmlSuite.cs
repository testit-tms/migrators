using System.Xml.Serialization;

namespace TestRailXmlExporter.Models;

[XmlRoot(ElementName = "suite")]
public record struct TestRailsXmlSuite
{
    [XmlElement(ElementName = "name")]
    public string? Name { get; set; }

    [XmlArray(ElementName = "sections")]
    [XmlArrayItem("section", Type = typeof(TestRailsXmlSection))]
    public TestRailsXmlSection[]? Sections { get; set; }
}
