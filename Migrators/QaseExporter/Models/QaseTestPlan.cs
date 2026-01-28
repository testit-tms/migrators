using System.Text.Json.Serialization;

namespace QaseExporter.Models;

public class QaseTestPlanResponse
{
    [JsonPropertyName("result")]
    public QaseTestPlan Plan { get; set; } = new();
}

public class QaseTestPlan
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = null!;
}
