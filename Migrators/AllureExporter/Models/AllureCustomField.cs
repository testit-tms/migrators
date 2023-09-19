using System.Text.Json.Serialization;

namespace AllureExporter.Models;

public class AllureCustomField
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("customField")]
    public CustomField CustomField { get; set; }
}

public class CustomField
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

