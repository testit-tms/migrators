using System.Text.Json.Serialization;

namespace AzureExporter.Models;

public class AzureSuite
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    //TODO: Add the Plan property
    //[JsonPropertyName("plan")]
    //public AzureTestPlan Plan { get; set; }
}

public class AzureSuites
{
    [JsonPropertyName("value")]
    public List<AzureSuite> Value { get; set; }
}
