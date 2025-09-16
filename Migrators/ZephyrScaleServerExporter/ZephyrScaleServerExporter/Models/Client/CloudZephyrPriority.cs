using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models.Client;

public class CloudZephyrPriority
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}
