using System.Text.Json.Serialization;

namespace AzureExporter.Models;

public class AzureStep
{
    [JsonPropertyName("parameterizedString")]
    public List<string> Values { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
}

public class AzureSteps
{
    [JsonPropertyName("steps")]
    public List<AzureStep> Steps { get; set; }

    [JsonPropertyName("compref")]
    public List<AzureWorkItem> SharedSteps { get; set; }
}
