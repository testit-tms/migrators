using System.Text.Json.Serialization;

namespace QaseExporter.Models;

public class QaseCaseStatsResponse
{
    [JsonPropertyName("stats")]
    public Dictionary<string, QaseCaseStat> StatMap { get; set; } = new();
}

public class QaseCaseStat
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("time")]
    public int Time { get; set; }
}

public class QaseTestResult
{
    [JsonPropertyName("case_id")]
    public int CaseId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [JsonPropertyName("parameters")]
    public List<QaseTestResultParameter> Parameters { get; set; } = new();

    [JsonPropertyName("is_qase_automated")]
    public bool Automated { get; set; }
}

public class QaseTestResultParameter
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [JsonPropertyName("value")]
    public string Value { get; set; } = null!;
}
