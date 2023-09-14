using System.Text.Json.Serialization;

namespace AzureExporter.Models;

public class AzureTestPoint
{
    [JsonPropertyName("testCase")]
    public AzureTestCase TestCase { get; set; }
}

public class AzureTestPoints
{
    [JsonPropertyName("value")]
    public List<AzureTestPoint> Value { get; set; }
}
