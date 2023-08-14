using System.Text.Json.Serialization;

namespace AllureExporter.Models;

public class AllureTestCase
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("description")]
    public string Description { get; set; }
    [JsonPropertyName("tags")]
    public Tags[] Tags { get; set; }
    [JsonPropertyName("status")]
    public Status Status { get; set; }
}

public class Tags
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class Status
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

