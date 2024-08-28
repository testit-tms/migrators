using System.Text.Json.Serialization;

namespace QaseExporter.Models;

public class QaseCustomField : QaseBaseField
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }
}

public class QaseFields
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("filtered")]
    public int Filtered { get; set; }

    [JsonPropertyName("entities")]
    public List<QaseCustomField> Fields { get; set; }
}

public class QaseFieldsData
{
    [JsonPropertyName("result")]
    public QaseFields FieldsData { get; set; }
}
