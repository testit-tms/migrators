using System.Xml.Serialization;

namespace TestRailImporter.Models;

[XmlRoot(ElementName = "step")]
public record TestRailsXmlStep
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
