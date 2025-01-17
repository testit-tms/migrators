using System.Text.Json.Serialization;

namespace AllureExporter.Models;

public class AllureRelationTarget
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;
}
