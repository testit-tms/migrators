using System.Text.Json.Serialization;

namespace Models;

public class Iteration
{
    [JsonRequired]
    [JsonPropertyName("parameters")]
    public List<Parameter> Parameters { get; set; } = new();
}
