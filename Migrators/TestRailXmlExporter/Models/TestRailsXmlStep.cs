using System.Xml.Serialization;

namespace TestRailXmlExporter.Models;

[XmlRoot(ElementName = "step")]
public record struct TestRailsXmlStep
{
    [XmlElement(ElementName = "content")]
    public string? Action { get; set; }

    [XmlElement(ElementName = "expected")]
    public string? Expected { get; set; }

    [XmlElement(ElementName = "additional_info")]
    public string? Comments { get; set; }

    [XmlElement(ElementName = "refs")]
    public string? TestData { get; set; }

    [XmlElement(ElementName = "shared_step_id")]
    public string? SharedStepId { get; set; }
}
