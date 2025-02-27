using System.Text.Json.Serialization;

namespace TestRailExporter.Models.Client;

public class TestRailProject
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class TestRailProjects : TastRailBaseEntity
{
    [JsonPropertyName("projects")]
    public List<TestRailProject> Projects { get; set; } = new();
}
