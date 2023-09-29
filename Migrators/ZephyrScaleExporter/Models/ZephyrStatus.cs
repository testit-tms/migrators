using System.Text.Json.Serialization;

namespace ZephyrScaleExporter.Models;

public class ZephyrStatus
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class ZephyrStatuses : BaseModel
{
    [JsonPropertyName("values")]
    public List<ZephyrStatus> Statuses { get; set; }
}
