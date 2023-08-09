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
}
