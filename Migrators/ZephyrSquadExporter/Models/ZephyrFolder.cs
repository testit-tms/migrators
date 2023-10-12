using System.Text.Json.Serialization;

namespace ZephyrSquadExporter.Models;

public class ZephyrFolder
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }
}
