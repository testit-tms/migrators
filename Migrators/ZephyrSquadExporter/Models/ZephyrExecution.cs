using System.Text.Json.Serialization;

namespace ZephyrSquadExporter.Models;

public class ZephyrExecutions
{
    [JsonPropertyName("searchResult")]
    public SearchResult SearchResult { get; set; }
}

public class SearchResult
{
    [JsonPropertyName("searchObjectList")]
    public List<ZephyrExecution>? Executions { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("currentOffset")]
    public int CurrentOffset { get; set; }
}

public class ZephyrExecution
{
    [JsonPropertyName("execution")]
    public Execution Execution { get; set; }

    [JsonPropertyName("issueKey")]
    public string IssueKey { get; set; }

    [JsonPropertyName("issueLabel")]
    public string IssueLabel { get; set; }

    [JsonPropertyName("issueSummary")]
    public string IssueSummary { get; set; }

    [JsonPropertyName("issueDescription")]
    public string IssueDescription { get; set; }
}

public class Execution
{
    [JsonPropertyName("issueId")]
    public int IssueId { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }
}
