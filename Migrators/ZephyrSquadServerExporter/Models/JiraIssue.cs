using System.Text.Json.Serialization;

namespace ZephyrSquadServerExporter.Models;

public class JiraIssue
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("fields")]
    public JiraIssueFields Fields { get; set; }
}

public class JiraIssueFields
{
    [JsonPropertyName("labels")]
    public List<string> labels { get; set; }

    [JsonPropertyName("priority")]
    public JiraPriorityField Priority { get; set; }

    [JsonPropertyName("attachment")]
    public List<IssueAttachment> Attachments { get; set; }
}
