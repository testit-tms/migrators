using System.Text.Json.Serialization;

namespace AzureExporter.Models;

public class WorkItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
}
