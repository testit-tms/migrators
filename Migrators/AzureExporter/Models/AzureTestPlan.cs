using System.Text.Json.Serialization;

namespace AzureExporter.Models;

public class AzureTestPlan
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
}

public class AzureTestPlans
{
    [JsonPropertyName("value")]
    public List<AzureTestPlan> Value { get; set; }
}
