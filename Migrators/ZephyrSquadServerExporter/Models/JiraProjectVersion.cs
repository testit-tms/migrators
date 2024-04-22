using System.Text.Json.Serialization;

namespace ZephyrSquadServerExporter.Models;

public class JiraProjectVersion
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("projectId")]
    public int ProjectId { get; set; }

    [JsonPropertyName("archived")]
    public bool Archived { get; set; }

    [JsonPropertyName("released")]
    public bool Released { get; set; }
}
