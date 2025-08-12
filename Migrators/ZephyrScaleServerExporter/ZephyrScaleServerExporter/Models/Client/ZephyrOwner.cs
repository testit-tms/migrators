using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models.Client;

public class ZephyrOwner
{
    [JsonPropertyName("key")]
    public required string Key { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("emailAddress")]
    public required string EmailAddress { get; set; }

    [JsonPropertyName("displayName")]
    public required string DisplayName { get; set; }
}
