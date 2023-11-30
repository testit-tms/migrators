using System.Text.Json.Serialization;

namespace SpiraTestExporter.Models;

public class SpiraStep
{
    [JsonPropertyName("TestStepId")]
    public int Id { get; set; }

    [JsonPropertyName("Position")]
    public int Position { get; set; }

    [JsonPropertyName("Description")]
    public string Description { get; set; }

    [JsonPropertyName("ExpectedResult")]
    public string ExpectedResult { get; set; }

    [JsonPropertyName("LinkedTestCaseId")]
    public int? LinkedId { get; set; }
}
