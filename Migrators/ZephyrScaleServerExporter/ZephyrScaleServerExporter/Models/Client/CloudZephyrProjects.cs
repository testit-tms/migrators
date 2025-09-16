using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models.Client;

public class CloudZephyrProjects
{
    [JsonPropertyName("values")]
    public List<CloudZephyrProject> Projects { get; set; }
}


