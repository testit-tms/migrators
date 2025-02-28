using System.Text.Json.Serialization;

namespace TestRailExporter.Models.Client;

public class TestRailStep
{
    [JsonPropertyName("content")]
    public string? Action { get; set; }

    [JsonPropertyName("expected")]
    public string? Expected { get; set; }

    [JsonPropertyName("shared_step_id")]
    public int? SharedStepId { get; set; }
}

public class TestRailScenario
{
    [JsonPropertyName("content")]
    public string Action { get; set; } = string.Empty;
}
