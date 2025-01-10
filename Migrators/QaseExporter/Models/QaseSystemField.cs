using System.Text.Json.Serialization;

namespace QaseExporter.Models;

public class QaseSystemField : QaseBaseField
{
    [JsonPropertyName("input_type")]
    public int Type { get; set; }
}

public class QaseSysFieldsData
{
    [JsonPropertyName("result")]
    public List<QaseSystemField> Fields { get; set; } = new();
}
