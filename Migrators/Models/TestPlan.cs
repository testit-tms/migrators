using System.Text.Json.Serialization;

namespace Models;

public class TestPlan
{
    [JsonPropertyName("id")]
    [JsonRequired]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; set; } = null!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("testRunIds")]
    public List<Guid> TestRunIds { get; set; } = new();

    [JsonPropertyName("manualTestResultIds")]
    public List<Guid> ManualTestResultIds { get; set; } = new();
}
