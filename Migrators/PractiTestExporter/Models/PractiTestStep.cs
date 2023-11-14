using System.Text.Json.Serialization;

namespace PractiTestExporter.Models;

public class StepAttributes
{
    [JsonPropertyName("test-id")]
    public int TestId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("expected-results")]
    public string ExpectedResults { get; set; }

    [JsonPropertyName("position")]
    public int Position { get; set; }

    [JsonPropertyName("test-to-call-id")]
    public int? TestToCallId { get; set; }
}

public class PractiTestStep
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("attributes")]
    public StepAttributes Attributes { get; set; }
}

public class PractiTestSteps
{
    [JsonPropertyName("data")]
    public List<PractiTestStep> Data { get; set; }
}
