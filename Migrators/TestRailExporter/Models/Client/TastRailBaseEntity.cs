using System.Text.Json.Serialization;

namespace TestRailExporter.Models.Client;

public class TastRailBaseEntity
{
    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }
}
