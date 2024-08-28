using System.Text.Json.Serialization;

namespace QaseExporter.Models;

public class QaseAttachment
{
    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("mime")]
    public string Type { get; set; }

    [JsonPropertyName("filename")]
    public string Name { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}

public class QaseDescriptionData
{
    public string Description { get; set; }
    public List<QaseAttachment> Attachments { get; set; }
}