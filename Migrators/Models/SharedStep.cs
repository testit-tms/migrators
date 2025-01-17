using System.Text.Json.Serialization;

namespace Models;

public class SharedStep
{
    [JsonPropertyName("id")]
    [JsonRequired]
    public Guid Id { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    [JsonRequired]
    public StateType State { get; set; }

    [JsonPropertyName("priority")]
    [JsonRequired]
    public PriorityType Priority { get; set; }

    [JsonPropertyName("steps")]
    public List<Step> Steps { get; set; } = new();

    [JsonPropertyName("attributes")]
    public List<CaseAttribute> Attributes { get; set; } = new();

    [JsonPropertyName("links")]
    public List<Link> Links { get; set; } = new();

    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; set; } = null!;

    [JsonPropertyName("sectionId")]
    [JsonRequired]
    public Guid SectionId { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("attachments")]
    public List<string> Attachments { get; set; } = new();
}

