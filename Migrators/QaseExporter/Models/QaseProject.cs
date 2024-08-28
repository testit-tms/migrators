using System.Text.Json.Serialization;

namespace QaseExporter.Models;

public class QaseProject
{
    [JsonPropertyName("code")]
    public string Key { get; set; }

    [JsonPropertyName("title")]
    public string Name { get; set; }
}

public class QaseProjectData
{
    [JsonPropertyName("result")]
    public QaseProject Project { get; set; }
}
