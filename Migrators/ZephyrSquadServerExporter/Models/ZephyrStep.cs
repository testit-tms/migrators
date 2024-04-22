using System.Text.Json.Serialization;

namespace ZephyrSquadServerExporter.Models;

public class ZephyrStep
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("step")]
    public string Step { get; set; }

    [JsonPropertyName("data")]
    public string Data { get; set; }

    [JsonPropertyName("result")]
    public string Result { get; set; }

    [JsonPropertyName("attachmentsMap")]
    public List<ZephyrAttachment> Attachments { get; set; }

}

public class ZephyrSteps
{
    [JsonPropertyName("stepBeanCollection")]
    public List<ZephyrStep> Steps { get; set; }
}
