using System.Text.Json.Serialization;

namespace Models;

public class Section
{
    [JsonPropertyName("id")]
    [JsonRequired]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; set; }

    [JsonPropertyName("preconditionSteps")]
    public List<Step> PreconditionSteps { get; set; }

    [JsonPropertyName("postconditionSteps")]
    public List<Step> PostconditionSteps { get; set; }

    [JsonPropertyName("sections")]
    public List<Section> Sections { get; set; }
}
