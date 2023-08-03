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
    
    [JsonPropertyName("parentId")]
    public Guid ParentId { get; set; }
    
    [JsonPropertyName("preconditionSteps")]
    public Step[] PreconditionSteps { get; set; }
    
    [JsonPropertyName("postconditionSteps")]
    public Step[] PostconditionSteps { get; set; }
}