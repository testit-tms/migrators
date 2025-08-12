using System.Text.Json.Serialization;

namespace Models;

public class Iteration
{
    [JsonRequired]
    [JsonPropertyName("parameters")]
    public List<Parameter> Parameters { get; set; }

    public override string ToString()
    {
        return $"{nameof(Parameters)}: {string.Join(",", Parameters)}";
    }
}
