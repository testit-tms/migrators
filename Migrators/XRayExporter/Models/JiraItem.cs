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

    [JsonPropertyName("summary")]
    public string Summary { get; set; }

    [JsonPropertyName("labels")]
    public List<string> Labels { get; set; }

    [JsonPropertyName("issuelinks")]
    public List<JiraLink> IssueLinks { get; set; }
}

public class Attachment
{
    [JsonPropertyName("filename")]
    public string FileName { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }
}
