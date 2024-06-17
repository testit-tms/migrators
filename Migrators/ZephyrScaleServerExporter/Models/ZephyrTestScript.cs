using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models;

public class ZephyrTestScript
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("steps")]
    public List<ZephyrStep>? Steps { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
