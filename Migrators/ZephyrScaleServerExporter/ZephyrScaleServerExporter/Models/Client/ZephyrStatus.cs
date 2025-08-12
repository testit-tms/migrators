using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models.Client;

public class ZephyrStatus
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }
}
