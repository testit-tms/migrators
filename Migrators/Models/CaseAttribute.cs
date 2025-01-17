using System.Text.Json.Serialization;

namespace Models;

public class CaseAttribute
{
    [JsonPropertyName("id")]
    [JsonRequired]
    public Guid Id { get; set; }

    [JsonPropertyName("value")]
    public object Value { get; set; } = null!;
}
