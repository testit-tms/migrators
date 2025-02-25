using System.Text.Json.Serialization;

namespace TestRailExporter.Models;

public class TestRailAttachment
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;
}

public class TestRailAttachments : TastRailBaseEntity
{
    [JsonPropertyName("attachments")]
    public List<TestRailAttachment> Attachments { get; set; } = new();
}
