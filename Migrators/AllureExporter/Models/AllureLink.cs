using System.Text.Json.Serialization;

namespace AllureExporter.Models;

public class AllureLink
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}


