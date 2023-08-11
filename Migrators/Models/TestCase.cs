using System.Text.Json.Serialization;

namespace Models;

public class TestCase
{
    [JsonPropertyName("id")]
    [JsonRequired]
    public Guid Id { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("state")]
    [JsonRequired]
    public StateType State { get; set; }

    [JsonPropertyName("priority")]
    [JsonRequired]
    public PriorityType Priority { get; set; }

    [JsonPropertyName("steps")]
    public Step[] Steps { get; set; }

    [JsonPropertyName("preconditionSteps")]
    public Step[] PreconditionSteps { get; set; }

    [JsonPropertyName("postconditionSteps")]
    public Step[] PostconditionSteps { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("attributes")]
    public CaseAttribute[] Attributes { get; set; }

    [JsonPropertyName("tags")]
    public string[] Tags { get; set; }

    [JsonPropertyName("attachments")]
    public string[] Attachments { get; set; }

    [JsonPropertyName("iterations")]
    public Iteration[] Iterations { get; set; }

    [JsonPropertyName("links")]
    public Link[] Links { get; set; }

    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; set; }

    [JsonPropertyName("sectionId")]
    [JsonRequired]
    public Guid SectionId { get; set; }
}
