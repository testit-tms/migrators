using System.Text.Json.Serialization;

namespace Models;

public class Link
{
    [JsonPropertyName("url")]
    [JsonRequired]
    public string Url { get; set; } = null!;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public LinkType Type { get; set; }
}
