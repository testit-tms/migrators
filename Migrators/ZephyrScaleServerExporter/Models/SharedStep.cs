using System.Text.Json.Serialization;

namespace Models;

public class SharedStep
{
    [JsonPropertyName("id")]
    [JsonRequired]
    public Guid Id { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("state")]
    [JsonRequired]
    public StateType State { get; set; }

    [JsonPropertyName("priority")]
    [JsonRequired]
    public PriorityType Priority { get; set; }

    [JsonPropertyName("steps")]
    public List<Step>? Steps { get; set; }

    [JsonPropertyName("attributes")]
    public List<CaseAttribute>? Attributes { get; set; }

    [JsonPropertyName("links")]
    public List<Link>? Links { get; set; }

    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; set; }

    [JsonPropertyName("sectionId")]
    [JsonRequired]
    public Guid SectionId { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("attachments")]
    public List<string>? Attachments { get; set; }
}

