using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models.Attachment;

public class AltAttachmentsResponse
{
    [JsonPropertyName("attachments")]
    public List<AltAttachmentResult> Attachments { get; set; } = [];
}
