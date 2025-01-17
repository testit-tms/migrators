using System.Text.Json.Serialization;

namespace AllureExporter.Models;

public class TcCommentsSection
{
    [JsonPropertyName("content")]
    public List<TcComment> Content { get; set; } = new();

    [JsonPropertyName("totalElements")]
    public long TotalElements { get; set; }

    [JsonPropertyName("totalPages")]
    public long TotalPages { get; set; }

    [JsonPropertyName("last")]
    public bool Last { get; set; }

    [JsonPropertyName("first")]
    public bool First { get; set; }

    [JsonPropertyName("size")]
    public int Size {get; set;}

    [JsonPropertyName("numberOfElements")]
    public long NumberOfElements { get; set; }
}
