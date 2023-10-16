using System.Text.Json.Serialization;

namespace ZephyrSquadExporter.Models;

public class ZephyrAttachment
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("fileExtension")]
    public string FileExtension { get; set; }
}
