using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models.Client;

public class CloudZephyrProject
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("key")]
    public string Key { get; set; }
}
