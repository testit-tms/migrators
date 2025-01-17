using System.Text.Json.Serialization;

namespace AllureExporter.Models;

public class AllureStep
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("attachments")]
    public List<AllureAttachment>? Attachments { get; set; }

    [JsonPropertyName("steps")]
    public List<AllureStep> Steps { get; set; } = new();

    [JsonPropertyName("keyword")]
    public string Keyword { get; set; } = string.Empty;

    [JsonPropertyName("expectedResult")]
    public string ExpectedResult { get; set; } = string.Empty;
}

public class AllureSteps
{
    [JsonPropertyName("steps")]
    public List<AllureStep> Steps { get; set; } = new();
}

public class AllureScenarioStep
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("attachmentId")]
    public long? AttachmentId { get; set; }

    [JsonPropertyName("children")]
    public List<long>? NestedStepIds { get; set; }

    [JsonPropertyName("expectedResult")]
    public string? ExpectedResult { get; set; }

    [JsonPropertyName("sharedStepId")]
    public long? SharedStepId { get; set; }
}

public class AllureStepsInfo : AllureSharedStepsInfo
{
    [JsonPropertyName("scenarioSteps")]
    public Dictionary<string, AllureScenarioStep> ScenarioStepsDictionary { get; set; } = new();

    [JsonPropertyName("attachments")]
    public Dictionary<string, AllureAttachment> AttachmentsDictionary { get; set; } = new();

    [JsonPropertyName("sharedSteps")]
    public Dictionary<string, AllureSharedStepInfo> SharedStepsDictionary { get; set; } = new();
}
