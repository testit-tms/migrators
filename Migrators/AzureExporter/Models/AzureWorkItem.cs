using System.Text.Json.Serialization;

namespace AzureExporter.Models;

public class AzureWorkItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("fields")]
    public Dictionary<string, dynamic> Fields { get; set; }

    //[JsonPropertyName("_links")]
    //public List<AzureLink> Links { get; set; }
}
