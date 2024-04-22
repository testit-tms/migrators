using System.Text.Json.Serialization;

namespace ZephyrSquadServerExporter.Models;

public class JiraIssueType
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }
}

public class JiraIssueTypes
{
    [JsonPropertyName("values")]
    public List<JiraIssueType> Types { get; set; }

    [JsonPropertyName("total")]
    public int Count { get; set; }
}
