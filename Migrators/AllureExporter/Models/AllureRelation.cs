using System.Text.Json.Serialization;

namespace AllureExporter.Models;

// [{id: 1, target: {id: 1,…}, type: "related to"}]
public class AllureRelation
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    [JsonPropertyName("target")]
    public AllureRelationTarget? Target { get; set; }
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

