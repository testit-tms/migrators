using System.Text.Json.Serialization;

namespace Models;

public class TestRun
{
    [JsonPropertyName("id")]
    [JsonRequired]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; set; } = null!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("AutotestResultIds")]
    public List<Guid> AutoTestResultIds { get; set; } = new();
}
