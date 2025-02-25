using System.Text.Json.Serialization;

namespace TestRailExporter.Models;

public class TestRailStep
{
    [JsonPropertyName("content")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("expected")]
    public string Expected { get; set; } = string.Empty;

    [JsonPropertyName("shared_step_id")]
    public int? SharedStepId { get; set; }
}

public class TestRailScenario
{
    [JsonPropertyName("content")]
    public string Action { get; set; } = string.Empty;
}
