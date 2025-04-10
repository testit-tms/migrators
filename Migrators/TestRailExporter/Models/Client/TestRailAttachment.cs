using System.Text.Json.Serialization;

namespace TestRailExporter.Models.Client;

public class TestRailAttachment
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("cassandra_file_id")]
    public string Guid { get; set; } = string.Empty;

    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;
}

public class TestRailAttachments : TastRailBaseEntity
{
    [JsonPropertyName("attachments")]
    public List<TestRailAttachment> Attachments { get; set; } = new();
}
