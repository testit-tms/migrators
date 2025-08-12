using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models.Client;

public class ZephyrConfluenceLink
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    public ZephyrConfluenceLink() { }
    public ZephyrConfluenceLink(string? title, string? url)
    {
        Title = title;
        Url = url;
    }
}
