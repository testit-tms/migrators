using System.Text.Json.Serialization;

namespace Models;

public class StepResult
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("outcome")]
    public Outcome? Outcome { get; set; }

    [JsonPropertyName("stepResults")]
    public List<StepResult> StepResults { get; set; } = new();

    [JsonPropertyName("attachments")]
    public List<string> Attachments { get; set; } = new();

    [JsonPropertyName("parameters")]
    public List<Parameter> Parameters { get; set; } = new();
}
