using System.Text.Json.Serialization;

namespace PractiTestExporter.Models;

public class ProjectAttributes
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class ProjectData
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("attributes")]
    public ProjectAttributes Attributes { get; set; }
}

public class PractiTestProject
{
    [JsonPropertyName("data")]
    public ProjectData Data { get; set; }
}
