using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models;

public class ZephyrStep
{
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("testData")]
    public string TestData { get; set; }

    [JsonPropertyName("expectedResult")]
    public string ExpectedResult { get; set; }

    [JsonPropertyName("customFieldValues")]
    public List<ZephyrCustomField>? CustomFields { get; set; }

    [JsonPropertyName("testCaseKey")]
    public string? TestCaseKey { get; set; }
}
