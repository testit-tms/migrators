using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models.Client;

public class ZephyrWebLink
{
    [JsonPropertyName("urlDescription")]
    public required string UrlDescription { get; set; }

    [JsonPropertyName("url")]
    public required string Url { get; set; }
}

public class ConfluencePageId
{
    [JsonPropertyName("confluencePageId")]
    public required string Id { get; set; }
}
