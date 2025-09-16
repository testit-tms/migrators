using System.Text.Json.Serialization;
using ZephyrScaleServerExporter.Models.TestCases;

namespace ZephyrScaleServerExporter.Models.Client;

public class CloudZephyrTestCases
{
    [JsonPropertyName("isLast")]
    public bool IsLast { get; set; }

    [JsonPropertyName("values")]
    public List<CloudZephyrTestCase> TestCases { get; set; }
}
