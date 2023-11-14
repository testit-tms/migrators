using System.Text.Json.Serialization;

namespace TestCollabExporter.Models;

public class TestCollabSharedStep
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("steps")]
    public List<Steps> Steps { get; set; }
}

