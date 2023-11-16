using System.Text.Json.Serialization;

namespace SpiraTestExporter.Models;

public class SpiraStepParameter
{
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [JsonPropertyName("Value")]
    public string Value { get; set; }
}

