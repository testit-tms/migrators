using System.Text.Json.Serialization;

namespace TestCollabExporter.Models;


public class TestCollabProject
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("company")]
    public int CompanyId { get; set; }
}



