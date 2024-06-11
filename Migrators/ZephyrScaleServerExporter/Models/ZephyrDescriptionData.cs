using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models;

public class ZephyrDescriptionData
{
    public string Description { get; set; }
    public List<ZephyrAttachment> Attachments { get; set; }
}

public class ZephyrAttachment
{
    [JsonPropertyName("filename")]
    public string FileName { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}
