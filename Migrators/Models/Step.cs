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
    public Step[] Steps { get; set; }

    [JsonPropertyName("attachments")]
    public string[] Attachments { get; set; }
}
