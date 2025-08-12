using System.Text.Json.Serialization;
using ZephyrScaleServerExporter.Models.TestCases;

namespace ZephyrScaleServerExporter.Models.Client;

public class ZephyrResultsResponse
{
    [JsonPropertyName("last")]
    public bool Last { get; set; }
    
    [JsonPropertyName("maxResults")]
    public int MaxResults { get; set; }

    [JsonPropertyName("results")]
    public List<ZephyrTestCase> Results { get; set; } = null!;

    [JsonPropertyName("startAt")]
    public int StartAt { get; set; }
    
    [JsonPropertyName("total")]
    public int Total { get; set; }
}