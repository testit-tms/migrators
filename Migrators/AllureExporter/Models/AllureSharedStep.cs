using System.Text.Json.Serialization;

namespace AllureExporter.Models;


public class AllureSharedSteps
{
    [JsonPropertyName("content")]
    public List<AllureSharedStep> Content { get; set; } = new();

    [JsonPropertyName("totalPages")]
    public long TotalPages { get; set; }
}

public class AllureSharedStepBase
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}

public class AllureSharedStep : AllureSharedStepBase
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class AllureSharedStepInfo : AllureSharedStepBase
{
    [JsonPropertyName("body")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("children")]
    public List<long> NestedStepIds { get; set; } = new();
}

public class AllureScenarioRoot
{
    [JsonPropertyName("children")]
    public List<long> NestedStepIds { get; set; } = new();
}

public class AllureSharedStepsInfo
{
    [JsonPropertyName("root")]
    public AllureScenarioRoot? Root { get; set; }

    [JsonPropertyName("sharedStepScenarioSteps")]
    public Dictionary<string, AllureScenarioStep> SharedStepScenarioStepsDictionary { get; set; } = new();

    [JsonPropertyName("sharedStepAttachments")]
    public Dictionary<string, AllureAttachment> SharedStepAttachmentsDictionary { get; set; } = new();
}
