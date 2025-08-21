using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace ZephyrScaleServerExporter.Models.Attachment;

public class AltAttachmentResult
{
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;
    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("id")]
    public long Id { get; set; }
    [JsonPropertyName("projectId")]
    public long ProjectId { get; set; }
    [JsonPropertyName("userKey")]
    public string UserKey { get; set; } = string.Empty;

    public ZephyrAttachment ToZephyrAttachment(IOptions<AppConfig> appConfig)
    {
        var url = appConfig.Value.Zephyr.Url.TrimEnd('/') + "/rest/tests/1.0/attachment/" + Id;
        return new ZephyrAttachment
        {
            FileName = FileName,
            Url = url
        };
    }
}
