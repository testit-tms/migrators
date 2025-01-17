using System.Text.Json.Serialization;

namespace Models;

public class Parameter
{
    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; set; } = null!;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}
