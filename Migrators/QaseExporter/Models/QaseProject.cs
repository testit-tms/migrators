using System.Text.Json.Serialization;

namespace QaseExporter.Models;

public class QaseProject
{
    [JsonPropertyName("code")]
    public string Key { get; set; } = null!;

    [JsonPropertyName("title")]
    public string Name { get; set; } = null!;
}

public class QaseProjectData
{
    [JsonPropertyName("result")]
    public QaseProject Project { get; set; } = null!;
}
