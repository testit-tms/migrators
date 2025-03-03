using System.Text.Json.Serialization;

namespace AllureExporter.Models.TestCase;

public class AllureCustomField
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("customField")] public CustomField? CustomField { get; set; }
}

public class CustomField
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
}
