using System.Text.Json.Serialization;

namespace ZephyrSquadServerExporter.Models;

public class ZephyrCycle
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("projectId")]
    public int ProjectId { get; set; }

    [JsonPropertyName("versionId")]
    public int VersionId { get; set; }

    public string Id { get; set; }
}
