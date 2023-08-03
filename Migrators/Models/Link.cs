using System.Text.Json.Serialization;

namespace Models;

public class Link
{
    [JsonPropertyName("url")]
    [JsonRequired]
    public string Url { get; set; }

    [JsonPropertyName("title")] 
    public string Title { get; set; }

    [JsonPropertyName("description")] 
    public string Description { get; set; }

    [JsonPropertyName("type")] 
    public LinkType Type { get; set; }
}