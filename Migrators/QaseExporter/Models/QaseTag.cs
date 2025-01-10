using System.Text.Json.Serialization;

namespace QaseExporter.Models;

public class QaseTag
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [JsonPropertyName("internal_id")]
    public int InternalId { get; set; }
}
