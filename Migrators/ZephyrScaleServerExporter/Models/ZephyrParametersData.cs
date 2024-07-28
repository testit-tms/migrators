using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models;

public class ZephyrParametersData
{
    [JsonPropertyName("paramType")]
    public string Type { get; set; }

    [JsonPropertyName("testData")]
    public List<Dictionary<string, object>> TestData { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("parameters")]
    public List<ZephyrParameter> Parameters { get; set; }
}

public class ParametersData
{
    public string Type { get; set; }
    public List<Dictionary<string, ZephyrDataParameter>> TestData { get; set; }
    public List<ZephyrParameter> Parameters { get; set; }
}

public class ZephyrDataParameter
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("dataSetItemId")]
    public int DataSetItemId { get; set; }
}

public class DataParameter
{
    List<ZephyrParameter> Parameters;
}

public class ZephyrParameter
{
    [JsonPropertyName("defaultValue")]
    public string Value { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }
}
