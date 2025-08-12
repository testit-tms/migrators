using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models.Client;

public class ZephyrProject
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("key")]
    public required string Key { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }
}
