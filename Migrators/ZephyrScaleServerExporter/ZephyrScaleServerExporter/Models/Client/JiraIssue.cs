using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models.Client;

public class JiraIssue
{
    [JsonPropertyName("self")]
    public required string Url { get; set; }

    [JsonPropertyName("key")]
    public required string Key { get; set; }

    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("fields")]
    public required JiraIssueFields Fields { get; set; }
}

public class JiraIssueFields
{
    [JsonPropertyName("summary")]
    public required string Name { get; set; }
}
