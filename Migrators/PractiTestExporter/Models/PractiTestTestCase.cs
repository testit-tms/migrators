using System.Text.Json.Serialization;

namespace PractiTestExporter.Models;

public class TestCaseAttributes
{
    [JsonPropertyName("project-id")]
    public int ProjectId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("preconditions")]
    public string Preconditions { get; set; }

    [JsonPropertyName("steps-count")]
    public int StepsCount { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("priority")]
    public object Priority { get; set; }

    [JsonPropertyName("duration-estimate")]
    public string DurationEstimate { get; set; }

    [JsonPropertyName("test-type")]
    public string TestType { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; }

    [JsonPropertyName("custom-fields")]
    public Dictionary<string, string> CustomFields { get; set; }
}

public class PractiTestTestCase
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("attributes")]
    public TestCaseAttributes Attributes { get; set; }
}

public class PractiTestTestCases
{
    [JsonPropertyName("data")]
    public List<PractiTestTestCase> Data { get; set; }
}

public class SinglePractiTestTestCase
{
    [JsonPropertyName("data")]
    public PractiTestTestCase Data { get; set; }
}
