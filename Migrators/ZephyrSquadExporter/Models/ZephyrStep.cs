using System.Text.Json.Serialization;

namespace ZephyrSquadExporter.Models;

public class ZephyrStep
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("step")]
    public string Step { get; set; }

    [JsonPropertyName("data")]
    public string Data { get; set; }

    [JsonPropertyName("result")]
    public string Result { get; set; }

    [JsonPropertyName("attachments")]
    public List<ZephyrAttachment> Attachments { get; set; }

}

public class ZephyrSteps
{
    [JsonPropertyName("testSteps")]
    public List<ZephyrStep> Steps { get; set; }
}
