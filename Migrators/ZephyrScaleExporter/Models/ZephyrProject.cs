using System.Text.Json.Serialization;

namespace ZephyrScaleExporter.Models;

public class ZephyrProject
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("key")]
    public string Key { get; set; }
}

public class ZephyrProjects : BaseModel
{
    [JsonPropertyName("values")]
    public List<ZephyrProject> Projects { get; set; }
}
