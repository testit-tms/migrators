using System.Text.Json.Serialization;

namespace AzureExporter.Models;

public class AzureTestCase
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}
