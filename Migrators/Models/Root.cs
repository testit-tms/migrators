using System.Text.Json.Serialization;

namespace Models;

public class Root
{
    [JsonPropertyName("projectName")]
    [JsonRequired]
    public string ProjectName { get; set; } = null!;

    [JsonPropertyName("attributes")]
    public List<Attribute> Attributes { get; set; } = new();

    [JsonPropertyName("sections")]
    [JsonRequired]
    public List<Section> Sections { get; set; } = new();

    [JsonPropertyName("sharedSteps")]
    [JsonRequired]
    public List<Guid> SharedSteps { get; set; } = new();

    [JsonPropertyName("testCases")]
    [JsonRequired]
    public List<Guid> TestCases { get; set; } = new();
}
