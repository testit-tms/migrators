using System.Text.Json.Serialization;

namespace QaseExporter.Models;

public class QaseTestRunResponse
{
    [JsonPropertyName("result")]
    public QaseTestRunResult Result { get; set; }
}

public class QaseTestRunResult
{
    [JsonPropertyName("filtered")]
    public int Filtered { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("entities")]
    public List<QaseTestRun> Entities { get; set; }
}

public class QaseTestRun
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("plan_id")]
    public string PlanId { get; set; } = string.Empty;
}

public class QaseTestRunInfo
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [JsonPropertyName("hash")]
    public string Hash { get; set; } = null!;
}
