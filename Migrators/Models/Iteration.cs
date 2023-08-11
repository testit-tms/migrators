using System.Text.Json.Serialization;

namespace Models;

public class Iteration
{
    [JsonRequired]
    [JsonPropertyName("parameters")]
    public Parameter[] Parameters { get; set; }
}
