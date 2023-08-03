using System.Text.Json.Serialization;

namespace Models;

public class Attribute
{
    [JsonPropertyName("id")]
    [JsonRequired]
    public Guid Id { get; set; }
    
    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; set; }
    
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
    public string[] Options { get; set; }
}