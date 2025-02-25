using System.Text.Json.Serialization;

namespace TestRailExporter.Models;

public class TestRailSharedStep
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("custom_steps_separated")]
    public List<TestRailStep> Steps { get; set; } = new();

    [JsonPropertyName("case_ids")]
    public List<int> CaseIds { get; set; } = new();
}

public class TestRailSharedSteps : TastRailBaseEntity
{
    [JsonPropertyName("shared_steps")]
    public List<TestRailSharedStep> SharedSteps { get; set; } = new();
}
