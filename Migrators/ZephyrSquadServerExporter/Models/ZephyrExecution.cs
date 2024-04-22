using System.Text.Json.Serialization;

namespace ZephyrSquadServerExporter.Models;

public class ZephyrExecutions
{
    [JsonPropertyName("executions")]
    public List<ZephyrExecution> Executions { get; set; }
}

public class ZephyrExecution
{
    [JsonPropertyName("issueId")]
    public int IssueId { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("summary")]
    public string Name { get; set; }

    [JsonPropertyName("label")]
    public string Labels { get; set; }

    [JsonPropertyName("issueDescription")]
    public string? Description { get; set; }
}
