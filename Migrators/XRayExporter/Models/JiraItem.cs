using System.Text.Json.Serialization;

namespace XRayExporter.Models;

public class JiraItem
{
    [JsonPropertyName("fields")]
    public Fields Fields { get; set; }
}

public class Fields
{
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("attachment")]
    public List<Attachment> Attachments { get; set; }

}

public class Attachment
{
    [JsonPropertyName("filename")]
    public string Filename { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }
}
