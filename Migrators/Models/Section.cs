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
    public Step[] PreconditionSteps { get; set; }

    [JsonPropertyName("postconditionSteps")]
    public Step[] PostconditionSteps { get; set; }

    [JsonPropertyName("sections")]
    public Section[] Sections { get; set; }
}
