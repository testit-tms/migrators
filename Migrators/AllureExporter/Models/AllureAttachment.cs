using System.Text.Json.Serialization;

namespace AllureExporter.Models;

public class AllureAttachment
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;
}


public class AllureAttachmentContent
{
    [JsonPropertyName("content")]
    public List<AllureAttachment> Content { get; set; }  = new ();

    [JsonPropertyName("totalPages")]
    public long TotalPages { get; set; }
}
