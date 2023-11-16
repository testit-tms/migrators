using System.Text.Json.Serialization;

namespace SpiraTestExporter.Models;

public class SpiraAttachment
{
    [JsonPropertyName("AttachmentId")]
    public int Id { get; set; }

    [JsonPropertyName("FilenameOrUrl")]
    public string Name { get; set; }
}


