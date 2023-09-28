using System.Xml.Serialization;

namespace AzureExporter.Models;

public class AzureParameters
{
    public string Keys { get; set; }
    public string Values { get; set; }
}

[XmlRoot("parameters")]
public class AzureParameterKeys
{
    [XmlElement("param")]
    public List<AzureParameterKey> Keys { get; set; }
}

public class AzureParameterKey
{
    [XmlAttribute("name")]
    public string Name { get; set; }
}
