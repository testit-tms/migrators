using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models.TestCases;

// ZephyrTestScript.Steps
public class ZephyrStep
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("testData")]
    public string? TestData { get; set; }

    [JsonPropertyName("expectedResult")]
    public string? ExpectedResult { get; set; }

    [JsonPropertyName("customFieldValues")]
    public List<ZephyrCustomField>? CustomFields { get; set; }

    [JsonPropertyName("testCaseKey")]
    public string? TestCaseKey { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("attachments")]
    public List<StepAttachment?>? Attachments { get; set; }

}
