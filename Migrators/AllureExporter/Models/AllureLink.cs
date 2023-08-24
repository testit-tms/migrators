using System.Text.Json.Serialization;

namespace AllureExporter.Models;

public class AllureLink
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("url")]
    public string Url { get; set; }
}


