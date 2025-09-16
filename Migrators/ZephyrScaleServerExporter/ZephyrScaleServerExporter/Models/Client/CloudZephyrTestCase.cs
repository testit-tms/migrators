using System.Text.Json.Serialization;
using ZephyrScaleServerExporter.Models.TestCases;

namespace ZephyrScaleServerExporter.Models.Client;


public class CloudZephyrTestCase
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("key")]
    public string Key { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("objective")]
    public string Description { get; set; }

    [JsonPropertyName("precondition")]
    public string Precondition { get; set; }

    [JsonPropertyName("labels")]
    public List<string> Labels { get; set; }

    [JsonPropertyName("priority")]
    public CloudPriority Priority { get; set; }

    [JsonPropertyName("status")]
    public CloudStatus Status { get; set; }

    [JsonPropertyName("links")]
    public CloudLinks Links { get; set; }

    [JsonPropertyName("testScript")]
    public CloudTestScript TestScript { get; set; }

    [JsonPropertyName("customFields")]
    public Dictionary<string, object> CustomFields { get; set; }
}


public class CloudPriority
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
}

public class CloudStatus
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
}

public class CloudLinks
{
    [JsonPropertyName("issues")]
    public List<Issues> Issues { get; set; }

    [JsonPropertyName("webLinks")]
    public List<WebLinks> WebLinks { get; set; }
}

public class CloudIssues
{
    [JsonPropertyName("target")]
    public string Target { get; set; }
}

public class CloudWebLinks
{
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
}

public class CloudTestScript
{
    [JsonPropertyName("self")]
    public string Self { get; set; }
}

