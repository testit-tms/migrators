using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models;

public class JiraIssue
{
    [JsonPropertyName("self")]
    public string Url { get; set; }

    [JsonPropertyName("fields")]
    public JiraIssueFields Fields { get; set; }
}

public class JiraIssueFields
{
    [JsonPropertyName("summary")]
    public string Name { get; set; }
}
