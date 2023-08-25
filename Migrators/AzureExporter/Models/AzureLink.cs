using System.Text.Json.Serialization;

namespace AzureExporter.Models;

public class AzureLink
{
    [JsonPropertyName("href")]
    public string Link { get; set; }
}
