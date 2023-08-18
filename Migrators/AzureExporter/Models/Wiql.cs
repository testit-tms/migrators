using System.Text.Json.Serialization;

namespace AzureExporter.Models;

public class Wiql
{
    [JsonPropertyName("workItems")]
    public List<WorkItem> workItems { get; set; }
}
