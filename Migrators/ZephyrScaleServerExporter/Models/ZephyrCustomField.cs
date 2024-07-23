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


public class ZephyrCustomFieldForTestCase
{
    [JsonPropertyName("archived")]
    public bool Archived { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("projectId")]
    public int ProjectId { get; set; }

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("options")]
    public List<ZephyrCustomFieldOption>? Options { get; set; }
}
