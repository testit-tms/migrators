using System.Text.Json.Serialization;

namespace HPALMExporter.Models;

public class HPALMItem
{
    [JsonPropertyName("logicalName")]
    public string LogicalName { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }
}

public class HPALMList
{
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [JsonPropertyName("Id")]
    public int Id { get; set; }

    [JsonPropertyName("LogicalName")]
    public string LogicalName { get; set; }

    [JsonPropertyName("Items")]
    public List<HPALMItem> Items { get; set; }
}

public class HPALMLists
{
    [JsonPropertyName("lists")]
    public List<HPALMList> Root { get; set; }
}
