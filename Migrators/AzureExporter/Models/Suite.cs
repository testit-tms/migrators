using System.Text.Json.Serialization;

namespace AzureExporter.Models;

public class Suite
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
}

public class Suites
{
    [JsonPropertyName("value")]
    public List<Suite> Value { get; set; }
}
