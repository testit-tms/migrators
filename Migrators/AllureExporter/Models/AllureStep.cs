using System.Text.Json.Serialization;

namespace AllureExporter.Models;

public class AllureStep
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("attachments")]
    public AllureAttachment[] Attachments { get; set; }

    [JsonPropertyName("steps")]
    public AllureStep[] Steps { get; set; }

    [JsonPropertyName("keyword")]
    public string Keyword { get; set; }
}

public class AllureSteps
{
    [JsonPropertyName("steps")]
    public List<AllureStep> Steps { get; set; }
}
