using System.Text.Json.Serialization;

namespace TestCollabExporter.Models;

public class TestCollabSuite
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("parent_id")]
    public int Parent_id { get; set; }
}
