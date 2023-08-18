using System.Text.Json.Serialization;

namespace AzureExporter.Models;

public class TestCase
{
    [JsonPropertyName("id")]
    public int id { get; set; }

    [JsonPropertyName("fields")]
    public Dictionary<string, string> fields { get; set; }

    [JsonPropertyName("_links")]
    public List<Link> links { get; set; }
}
