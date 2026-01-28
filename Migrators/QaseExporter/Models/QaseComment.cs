using System.Text.Json.Serialization;

namespace QaseExporter.Models;

public class QaseCommentResponse
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("items")]
    public List<string> Comments { get; set; } = new();
}
