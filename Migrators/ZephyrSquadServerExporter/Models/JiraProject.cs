using System.Text.Json.Serialization;

namespace ZephyrSquadServerExporter.Models;

public class JiraProject
{
    [JsonPropertyName("key")]
    public string Key { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("versions")]
    public List<JiraProjectVersion> Versions { get; set; }

    [JsonPropertyName("issueTypes")]
    public List<JiraIssueType> IssueTypes { get; set; }
}
