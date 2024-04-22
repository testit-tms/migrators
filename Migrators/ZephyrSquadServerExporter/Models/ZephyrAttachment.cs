using System.Text.Json.Serialization;

namespace ZephyrSquadServerExporter.Models;

public class IssueAttachment
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("filename")]
    public string Name { get; set; }
}

public class ZephyrAttachment
{
    [JsonPropertyName("fileId")]
    public string Id { get; set; }

    [JsonPropertyName("fileName")]
    public string Name { get; set; }
}
