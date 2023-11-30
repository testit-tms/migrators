using System.Text.Json.Serialization;

namespace SpiraTestExporter.Models;

public class SpiraProject
{
    [JsonPropertyName("ProjectId")]
    public int Id { get; set; }

    [JsonPropertyName("ProjectTemplateId")]
    public int TemplateId { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; }
}

