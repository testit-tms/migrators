using System.Text.Json.Serialization;

namespace ZephyrSquadServerExporter.Models;

public class JiraPriorityField
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }
}
