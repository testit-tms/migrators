using System.Text.Json.Serialization;

namespace ZephyrScaleExporter.Models;

public class BaseModel
{
    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("startAt")]
    public int StartAt { get; set; }

    [JsonPropertyName("maxResults")]
    public int MaxResults { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("isLast")]
    public bool IsLast { get; set; }
}

