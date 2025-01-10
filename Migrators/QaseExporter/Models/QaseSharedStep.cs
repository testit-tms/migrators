using System.Text.Json.Serialization;

namespace QaseExporter.Models;

public class QaseSharedStep
{
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = null!;

    [JsonPropertyName("title")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("steps")]
    public List<QaseStep> Steps { get; set; } = new();
}

public class QaseSharedSteps
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("filtered")]
    public int Filtered { get; set; }

    [JsonPropertyName("entities")]
    public List<QaseSharedStep> SharedSteps { get; set; } = new();
}

public class SharedStepData
{
    [JsonPropertyName("result")]
    public QaseSharedSteps SharedStepsData { get; set; } = null!;
}
