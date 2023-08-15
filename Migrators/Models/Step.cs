using System.Net.Mail;
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

    [JsonPropertyName("steps")]
    public List<Step> Steps { get; set; }

    [JsonPropertyName("attachments")]
    public List<string> Attachments { get; set; }
}
