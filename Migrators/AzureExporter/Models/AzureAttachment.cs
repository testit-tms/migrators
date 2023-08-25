using System.Text.Json.Serialization;

namespace AzureExporter.Models;

public class AzureAttachment
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}
