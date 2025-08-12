using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models.Client;

public class JiraComponent
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}
