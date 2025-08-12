using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models.Attachment;

public class ZephyrDescriptionData
{
    public required string Description { get; set; }
    public required List<ZephyrAttachment> Attachments { get; set; }
}

public class ZephyrAttachment
{
    [JsonPropertyName("filename")]
    public required string FileName { get; set; }

    [JsonPropertyName("url")]
    public required string Url { get; set; }
}
