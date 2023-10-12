using System.Text.Json.Serialization;

namespace ZephyrSquadExporter.Models;

public class ZephyrCycle
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }
}
