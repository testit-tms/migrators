using System.Text.Json.Serialization;

namespace TestRailExporter.Models.Client;

public class TestRailSection
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parent_id")]
    public int? ParentId { get; set; }

    [JsonPropertyName("suite_id")]
    public int? SuiteId { get; set; }
}

public class TestRailSections : TastRailBaseEntity
{
    [JsonPropertyName("sections")]
    public List<TestRailSection> Sections { get; set; } = new();
}
