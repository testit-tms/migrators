using System.Text.Json.Serialization;

namespace HPALMExporter.Models;

public class HPALMField
{
    [JsonPropertyName("required")] public bool Required { get; set; }

    [JsonPropertyName("system")] public bool System { get; set; }

    [JsonPropertyName("type")] public string Type { get; set; }

    [JsonPropertyName("active")] public bool Active { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("label")] public string Label { get; set; }

    [JsonPropertyName("physicalName")] public string PhysicalName { get; set; }

    [JsonPropertyName("listId")] public int? ListId { get; set; }
}

public class HPALMFields
{
    [JsonPropertyName("Field")] public List<HPALMField> Field { get; set; }
}

public class HPALMAttributes
{
    [JsonPropertyName("Fields")] public HPALMFields Fields { get; set; }
}
