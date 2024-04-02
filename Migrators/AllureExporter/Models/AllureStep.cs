using System.Text.Json.Serialization;

namespace AllureExporter.Models;

public class AllureStep
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("attachments")]
    public List<AllureAttachment>? Attachments { get; set; }

    [JsonPropertyName("steps")]
    public List<AllureStep> Steps { get; set; }

    [JsonPropertyName("keyword")]
    public string Keyword { get; set; }

    [JsonPropertyName("expectedResult")]
    public string ExpectedResult { get; set; }
}

public class AllureSteps
{
    [JsonPropertyName("steps")]
    public List<AllureStep> Steps { get; set; }
}

public class AllureScenarioRoot
{
    [JsonPropertyName("children")]
    public List<int> NestedStepIds { get; set; }
}

public class AllureSharedStep
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("body")]
    public string Name { get; set; }

    [JsonPropertyName("children")]
    public List<int> NestedStepIds { get; set; }
}

public class AllureScenarioStep
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("attachmentId")]
    public int? AttachmentId { get; set; }

    [JsonPropertyName("children")]
    public List<int>? NestedStepIds { get; set; }

    [JsonPropertyName("expectedResult")]
    public string? ExpectedResult { get; set; }

    [JsonPropertyName("sharedStepId")]
    public int? SharedStepId { get; set; }
}

public class AllureStepsInfo
{
    [JsonPropertyName("root")]
    public AllureScenarioRoot Root { get; set; }

    [JsonPropertyName("scenarioSteps")]
    public Dictionary<string, AllureScenarioStep> ScenarioStepsDictionary { get; set; }

    [JsonPropertyName("attachments")]
    public Dictionary<string, AllureAttachment> AttachmentsDictionary { get; set; }

    [JsonPropertyName("sharedSteps")]
    public Dictionary<string, AllureSharedStep> SharedStepsDictionary { get; set; }

    [JsonPropertyName("sharedStepScenarioSteps")]
    public Dictionary<string, AllureScenarioStep> SharedStepScenarioStepsDictionary { get; set; }

    [JsonPropertyName("sharedStepAttachments")]
    public Dictionary<string, AllureAttachment> SharedStepAttachmentsDictionary { get; set; }
}
