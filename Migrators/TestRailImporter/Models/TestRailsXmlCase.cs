using System.Xml.Serialization;

namespace TestRailImporter.Models;

[XmlRoot(ElementName = "case")]
public record struct TestRailsXmlCase
{
    [XmlElement(ElementName = "id")]
    public string? Id { get; set; }

    [XmlElement(ElementName = "title")]
    public string? Title { get; set; }

    [XmlElement(ElementName = "template")]
    public string? Template { get; set; }

    [XmlElement(ElementName = "type")]
    public string? Type { get; set; }

    [XmlElement(ElementName = "priority")]
    public string? Priority { get; set; }

    [XmlElement(ElementName = "estimate")]
    public string? Estimate { get; set; }

    [XmlElement(ElementName = "custom")]
    public TestRailsXmlCaseData? Custom { get; set; }

    [XmlElement(ElementName = "state")]
    public string? State { get; set; }

    [XmlElement(ElementName = "references")]
    public string? References { get; set; }
}
