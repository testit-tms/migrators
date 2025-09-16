using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models.Client;

public class CloudZephyrPriorities
{
    [JsonPropertyName("values")]
    public List<CloudZephyrPriority> Priorities { get; set; }
}
