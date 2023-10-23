using System.Text.Json.Serialization;

namespace XRayExporter.Models;

public class JiraProject
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("key")]
    public string Key { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}

