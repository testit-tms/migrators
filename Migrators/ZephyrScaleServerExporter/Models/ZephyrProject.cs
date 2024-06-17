using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models;

public class ZephyrProject
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("key")]
    public string Key { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}
