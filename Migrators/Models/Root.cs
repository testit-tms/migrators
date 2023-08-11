using System.Text.Json.Serialization;

namespace Models;

public class Root
{
    [JsonPropertyName("projectName")]
    [JsonRequired]
    public string ProjectName { get; set; }

    [JsonPropertyName("attributes")]
    public Attribute[] Attributes { get; set; }

    [JsonPropertyName("sections")]
    [JsonRequired]
    public Section[] Sections { get; set; }

    [JsonPropertyName("sharedSteps")]
    [JsonRequired]
    public Guid[] SharedSteps { get; set; }

    [JsonPropertyName("testCases")]
    [JsonRequired]
    public Guid[] TestCases { get; set; }
}
