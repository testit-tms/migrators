using System.Text.Json.Serialization;

namespace PractiTestExporter.Models;

public class AttachmentAttributes
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class PractiTestAttachment
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("attributes")]
    public AttachmentAttributes Attributes { get; set; }
}

public class PractiTestAttachments
{
    [JsonPropertyName("data")]
    public List<PractiTestAttachment> Data { get; set; }
}
