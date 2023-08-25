using System.Text.Json.Serialization;

namespace AzureExporter.Models;

public class TestPlan
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
}

public class TestPlans
{
    [JsonPropertyName("value")]
    public List<TestPlan> Value { get; set; }
}
