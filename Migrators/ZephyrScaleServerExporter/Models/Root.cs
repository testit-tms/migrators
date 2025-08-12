using System.Text.Json.Serialization;

namespace Models;

public class Root
{
    [JsonPropertyName("projectName")]
    [JsonRequired]
    public string ProjectName { get; set; }

    [JsonPropertyName("attributes")]
    public List<Attribute> Attributes { get; set; }

    [JsonPropertyName("sections")]
    [JsonRequired]
    public List<Section> Sections { get; set; }

    [JsonPropertyName("sharedSteps")]
    [JsonRequired]
    public List<Guid> SharedSteps { get; set; }

    [JsonPropertyName("testCases")]
    [JsonRequired]
    public List<Guid> TestCases { get; set; }
}
