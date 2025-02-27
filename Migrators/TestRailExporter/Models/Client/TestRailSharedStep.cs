using System.Text.Json.Serialization;

namespace TestRailExporter.Models.Client;

public class TestRailSharedStep
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("custom_steps_separated")]
    public List<TestRailStep>? Steps { get; set; }
}

public class TestRailSharedSteps : TastRailBaseEntity
{
    [JsonPropertyName("shared_steps")]
    public List<TestRailSharedStep> SharedSteps { get; set; } = new();
}
