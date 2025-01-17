using System.Text.Json.Serialization;

namespace Models;

public class Step
{
    [JsonPropertyName("sharedStepId")]
    public Guid? SharedStepId { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("expected")]
    public string Expected { get; set; } = string.Empty;

    [JsonPropertyName("actionAttachments")]
    public List<string> ActionAttachments { get; set; } = new();

    [JsonPropertyName("expectedAttachments")]
    public List<string> ExpectedAttachments { get; set; } = new();

    [JsonPropertyName("testDataAttachments")]
    public List<string> TestDataAttachments { get; set; } = new();

    [JsonPropertyName("testData")]
    public string TestData { get; set; } = string.Empty;
}
