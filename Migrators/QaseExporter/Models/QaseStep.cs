using System.Text.Json.Serialization;

namespace QaseExporter.Models;

public class QaseStep
{
    [JsonPropertyName("action")]
    public string Action { get; set; }

    [JsonPropertyName("expected_result")]
    public string? ExpectedResult { get; set; }

    [JsonPropertyName("data")]
    public string? Data { get; set; }

    [JsonPropertyName("steps")]
    public List<QaseStep>? Steps { get; set; }

    [JsonPropertyName("hash")]
    public string? Hash { get; set; }

    [JsonPropertyName("shared_step_hash")]
    public string? SharedStepHash { get; set; }

    [JsonPropertyName("shared_step_nested_hash")]
    public string? SharedStepNestedHash { get; set; }
}
