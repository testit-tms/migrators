using System.Text.Json.Serialization;

namespace AzureExporter.Models;

public class AzureTestCase
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("fields")]
    public Dictionary<string, dynamic> Fields { get; set; }

    [JsonPropertyName("_links")]
    public List<AzureLink> Links { get; set; }
}

public class AzureTestCases
{
    [JsonPropertyName("value")]
    public List<AzureTestCase> Value { get; set; }
}
