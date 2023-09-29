using System.Text.Json.Serialization;

namespace ZephyrScaleExporter.Models;

public class ZephyrTestCase
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("key")]
    public string Key { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("objective")]
    public string Description { get; set; }

    [JsonPropertyName("labels")]
    public List<string> Labels { get; set; }

    [JsonPropertyName("priority")]
    public Priority Priority { get; set; }

    [JsonPropertyName("status")]
    public Status Status { get; set; }

    [JsonPropertyName("links")]
    public Links Links { get; set; }
}

public class Priority
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
}

public class Status
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
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

public class ZephyrTestCases
{
    [JsonPropertyName("values")]
    public List<ZephyrTestCase> TestCases { get; set; }
}
