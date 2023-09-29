using System.Text.Json.Serialization;

namespace ZephyrScaleExporter.Models;

public class ZephyrStep
{
    [JsonPropertyName("inline")]
    public Inline Inline { get; set; }
}

public class Inline
{
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("testData")]
    public string TestData { get; set; }

    [JsonPropertyName("expectedResult")]
    public string ExpectedResult { get; set; }
}

public class ZephyrSteps : BaseModel
{
    [JsonPropertyName("values")]
    public List<ZephyrStep> Steps { get; set; }
}
