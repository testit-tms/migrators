using System.Text.Json.Serialization;

namespace TestRailExporter.Models.Client;

public class TestRailCase
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("section_id")]
    public int SectionId { get; set; }

    [JsonPropertyName("template_id")]
    public int TemplateId { get; set; }

    [JsonPropertyName("type_id")]
    public int TypeId { get; set; }

    [JsonPropertyName("priority_id")]
    public int PriorityId { get; set; }

    [JsonPropertyName("is_deleted")]
    public int IsDeleted { get; set; }

    [JsonPropertyName("custom_preconds")]
    public string? TextPreconds { get; set; }

    [JsonPropertyName("custom_steps")]
    public string? TextSteps { get; set; }

    [JsonPropertyName("custom_expected")]
    public string? TextExpected { get; set; }

    [JsonPropertyName("custom_mission")]
    public string? TextMission { get; set; }

    [JsonPropertyName("custom_goals")]
    public string? TextGoals { get; set; }

    [JsonPropertyName("custom_testrail_bdd_scenario")]
    public string? TextScenarios { get; set; }

    [JsonPropertyName("custom_steps_separated")]
    public List<TestRailStep>? Steps { get; set; }
}

public class TestRailCases : TastRailBaseEntity
{
    [JsonPropertyName("cases")]
    public List<TestRailCase> Cases { get; set; } = new();
}
