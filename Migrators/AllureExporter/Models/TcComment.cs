using System.Text.Json.Serialization;

namespace AllureExporter.Models;

public class TcComment
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("testCaseId")]
    public long TestCaseId { get; set; }

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("bodyHtml")]
    public string BodyHtml { get; set; } = string.Empty;

    [JsonPropertyName("createdDate")]
    public long CreatedDate { get; set; }

    [JsonPropertyName("lastModifiedDate")]
    public long LastModifiedDate { get; set; }

    [JsonPropertyName("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    [JsonPropertyName("lastModifiedBy")]
    public string LastModifiedBy { get; set; } = string.Empty;
}
