using System.Text.Json.Serialization;

namespace TestCollabExporter.Models;

public class TestCollabTestCase
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("steps")]
    public List<Steps> Steps { get; set; }

    [JsonPropertyName("priority")]
    public string Priority { get; set; }

    [JsonPropertyName("custom_fields")]
    public object[] CustomFields { get; set; }

    [JsonPropertyName("avg_execution_time")]
    public int ExecutionTime { get; set; }

    [JsonPropertyName("attachments")]
    public List<Attachments> Attachments { get; set; }

    [JsonPropertyName("tags")]
    public List<Tags> Tags { get; set; }
}

public class Steps
{
    [JsonPropertyName("step")]
    public string Step { get; set; }

    [JsonPropertyName("expected_result")]
    public string ExpectedResult { get; set; }

    [JsonPropertyName("reusable_step_id")]
    public int? ReusableStepId { get; set; }
}

public class Attachments
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}

public class Tags
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}
