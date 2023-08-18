using System.Text.Json.Serialization;

namespace AzureExporter.Models;

public class Link
{
    [JsonPropertyName("href")]
    public int link { get; set; }
}
