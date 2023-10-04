using System.Text.Json.Serialization;

namespace Models;

public class Step
{
    [JsonPropertyName("sharedStepId")]
    public Guid? SharedStepId { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; }

    [JsonPropertyName("expected")]
    public string Expected { get; set; }

    [JsonPropertyName("actionAttachments")]
    public List<string> ActionAttachments { get; set; }

    [JsonPropertyName("expectAttachments")]
    public List<string> ExpectAttachments { get; set; }

    [JsonPropertyName("testDataAttachments")]
    public List<string> TestDataAttachments { get; set; }

    [JsonPropertyName("testData")]
    public string TestData { get; set; }
}
