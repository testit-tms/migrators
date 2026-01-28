using System.Text.Json.Serialization;

namespace QaseExporter.Models;

public class QaseAuthor
{
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("result")]
    public QaseAuthorResult Result { get; set; } = new();
}

public class QaseAuthorResult
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("author_id")]
    public int AuthorId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("email")]
    public string Email { get; set; } = null!;

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }
}
