using System.Text.Json.Serialization;

namespace Models;

public class TestResult
{
    [JsonPropertyName("id")]
    [JsonRequired]
    public Guid Id { get; set; }

    [JsonPropertyName("testCaseId")]
    [JsonRequired]
    public Guid TestCaseId { get; set; }

    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; set; } = null!;

    [JsonPropertyName("statusCode")]
    [JsonRequired]
    public Outcome StatusCode { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("stepResults")]
    public List<StepResult> StepResults { get; set; } = new();

    [JsonPropertyName("setupResults")]
    public List<StepResult> SetupResults { get; set; } = new();

    [JsonPropertyName("teardownResults")]
    public List<StepResult> TeardownResults { get; set; } = new();

    [JsonPropertyName("links")]
    public List<Link> Links { get; set; } = new();

    [JsonPropertyName("attachments")]
    public List<string> Attachments { get; set; } = new();

    [JsonPropertyName("parameters")]
    public List<Parameter> Parameters { get; set; } = new();
}
