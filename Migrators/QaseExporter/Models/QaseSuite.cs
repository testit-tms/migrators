using System.Text.Json.Serialization;

namespace QaseExporter.Models;

public class QaseSuite
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("preconditions")]
    public string Preconditions { get; set; }

    [JsonPropertyName("cases_count")]
    public int CasesCount { get; set; }

    [JsonPropertyName("parent_id")]
    public int? ParentId { get; set; }
}

public class QaseSuites
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("filtered")]
    public int Filtered { get; set; }

    [JsonPropertyName("entities")]
    public List<QaseSuite> Suites { get; set; }
}

public class QaseSuitesData
{
    [JsonPropertyName("result")]
    public QaseSuites SuitesData { get; set; }
}
