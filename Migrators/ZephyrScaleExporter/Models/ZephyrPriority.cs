using System.Text.Json.Serialization;

namespace ZephyrScaleExporter.Models;

public class ZephyrPriority
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class ZephyrPriorities : BaseModel
{
    [JsonPropertyName("values")]
    public List<ZephyrPriority> Priorities { get; set; }
}
