using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

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
    public List<string> Options { get; set; }

    public override string ToString()
    {
        return
            $"{nameof(Id)}: {Id}, {nameof(Name)}: {Name}, {nameof(Type)}: {Type}, " +
            $"{nameof(IsRequired)}: {IsRequired}, {nameof(IsActive)}: {IsActive}, " +
            $"{nameof(Options)}: \"{String.Join(", ", Options.ToArray())}\"";
    }
}
