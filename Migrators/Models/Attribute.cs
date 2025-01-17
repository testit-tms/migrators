using System.Text.Json.Serialization;

namespace Models;

public class Attribute
{
    [JsonPropertyName("id")]
    [JsonRequired]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; set; } = null!;

    [JsonPropertyName("type")]
    [JsonRequired]
    public AttributeType Type { get; set; }

    [JsonPropertyName("isRequired")]
    [JsonRequired]
    public bool IsRequired { get; set; }

    [JsonPropertyName("isActive")]
    [JsonRequired]
    public bool IsActive { get; set; }

    [JsonPropertyName("options")]
    public List<string> Options { get; set; } = new();
}
