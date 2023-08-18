using System.Text.Json.Serialization;

namespace AzureExporter.Models;

public class WorkItem
{
    [JsonPropertyName("id")]
    public int id { get; set; }
}
