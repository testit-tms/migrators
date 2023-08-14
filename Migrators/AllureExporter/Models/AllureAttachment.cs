using System.Text.Json.Serialization;

namespace AllureExporter.Models;

public class AllureAttachment
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }
}
