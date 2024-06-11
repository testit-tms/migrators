using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models;

public class JiraComponent
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}
