using System.Text.Json.Serialization;

namespace ZephyrScaleExporter.Models;

public class ZephyrTestScript
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
}
