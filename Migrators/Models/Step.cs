using System.Text.Json.Serialization;

namespace Models;

public class Step
{
    [JsonPropertyName("action")]
    public string Action { get; set; }
    
    [JsonPropertyName("expected")]
    public string Expected { get; set; }
}