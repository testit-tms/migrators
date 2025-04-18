using System.Text.Json.Serialization;

namespace QaseExporter.Models;

public class QaseBaseField
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [JsonPropertyName("is_required")]
    public bool Required { get; set; }

    [JsonPropertyName("options")]
    public List<QaseOption> Options { get; set; } = new();
}

public class QaseOption
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;
}
