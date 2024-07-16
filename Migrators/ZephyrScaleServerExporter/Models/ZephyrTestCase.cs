using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models;

public class ZephyrTestCase
{

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
    public string Priority { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("issueLinks")]
    public List<string>? IssueLinks { get; set; }

    [JsonPropertyName("testScript")]
    public ZephyrTestScript TestScript { get; set; }

    [JsonPropertyName("customFields")]
    public Dictionary<string, object> CustomFields { get; set; }

    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; }

    [JsonPropertyName("folder")]
    public string? Folder { get; set; }

    [JsonPropertyName("component")]
    public string? Component { get; set; }
}

public class ZephyrArchivedTestCase
{

    [JsonPropertyName("key")]
    public string Key { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("testScript")]
    public ZephyrArchivedTestScript TestScript { get; set; }
}

public class Links
{
    [JsonPropertyName("issues")]
    public List<Issues> Issues { get; set; }

    [JsonPropertyName("webLinks")]
    public List<WebLinks> WebLinks { get; set; }
}

public class Issues
{
    [JsonPropertyName("target")]
    public string Target { get; set; }
}

public class WebLinks
{
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
}
