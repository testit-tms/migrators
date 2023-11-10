using System.Text.Json.Serialization;

namespace TestCollabExporter.Models;

public class TestCollabCustomField
{
    [JsonPropertyName("label")]
    public string Label { get; set; }

    [JsonPropertyName("default_value")]
    public string DefaultValue { get; set; }

    [JsonPropertyName("is_required")]
    public bool IsRequired { get; set; }

    [JsonPropertyName("extra")]
    public Extra Extra { get; set; }
}

public class Extra
{
    [JsonPropertyName("options")]
    public List<Options> Options { get; set; }
}

public class Options
{
    [JsonPropertyName("label")]
    public string Value { get; set; }
}

