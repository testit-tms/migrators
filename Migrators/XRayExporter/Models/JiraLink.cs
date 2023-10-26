using System.Text.Json.Serialization;

namespace XRayExporter.Models;

public class JiraLink
{
    [JsonPropertyName("type")]
    public Type Type { get; set; }

    [JsonPropertyName("inwardIssue")]
    public Issue? InwardIssue { get; set; }

    [JsonPropertyName("outwardIssue")]
    public Issue? OutwardIssue { get; set; }
}

public class Type
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("inward")]
    public string Inward { get; set; }
}

public class Issue
{
    [JsonPropertyName("key")]
    public string Key { get; set; }

    [JsonPropertyName("self")]
    public string Self { get; set; }
}

