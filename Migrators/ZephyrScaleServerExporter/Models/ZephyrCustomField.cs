using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models;

public class ZephyrCustomField
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("customField")]
    public ZephyrCustomFieldData CustomField { get; set; }

    [JsonPropertyName("intValue")]
    public int? IntValue { get; set; }

    [JsonPropertyName("stringValue")]
    public string? StringValue { get; set; }
}

public class ZephyrCustomFieldData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("options")]
    public List<ZephyrCustomFieldOption>? Options { get; set; }
}

public class ZephyrCustomFieldOption
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}
