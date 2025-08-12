using System.Text.Json.Serialization;

namespace Models;

public class Parameter
{
    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }

    public override string ToString()
    {
        return $"{{{nameof(Name)}: {Name}, {nameof(Value)}: {Value}}}";
    }
}
