using System.Text.Json.Serialization;

namespace TestRailExporter.Models.Client;

public class TestRailCaseStatus
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("short_name")]
    public string ShortName { get; set; } = string.Empty;
}
