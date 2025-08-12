using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models.TestCases;

// many models
public class ZephyrTestScript
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("steps")]
    public List<ZephyrStep>? Steps { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class ZephyrArchivedTestScript
{
    [JsonPropertyName("stepByStepScript")]
    public ZephyrStepByStepScript? StepScript { get; set; }

    [JsonPropertyName("plainTextScript")]
    public ZephyrTextScript? TextScript { get; set; }
}

public class ZephyrStepByStepScript
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("steps")]
    public List<ZephyrStep>? Steps { get; set; }
}

public class ZephyrTextScript
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
