using System.Xml;
using System.Xml.Serialization;

namespace TestRailImporter.Models;

[XmlRoot(ElementName = "custom")]
public record TestRailsXmlCaseData
{
    [XmlElement(ElementName = "comment")]
    public string? Comments { get; set; }

    [XmlElement(ElementName = "preconds")]
    public string? Preconditions { get; set; }

    public List<TestRailsXmlStep> Steps => StepsSeparated.Concat(StepsCases).ToList();

    [XmlArray(ElementName = "steps_separated")]
    [XmlArrayItem(ElementName = "step")]
    public TestRailsXmlStep[] StepsSeparated { get; set; } = Array.Empty<TestRailsXmlStep>();

    [XmlArray(ElementName = "steps_case")]
    [XmlArrayItem(ElementName = "step")]
    public TestRailsXmlStep[] StepsCases { get; set; } = Array.Empty<TestRailsXmlStep>();

    [XmlElement(ElementName = "steps")]
    public string? Step { get; set; }

    [XmlElement(ElementName = "expected")]
    public string? Expected { get; set; }

    [XmlElement(ElementName = "mission")]
    public string? Mission { get; set; }

    [XmlElement(ElementName = "goals")]
    public string? Goals { get; set; }

    [XmlAnyElement]
    public XmlElement[]? CustomAttributes { get; set; }
}
