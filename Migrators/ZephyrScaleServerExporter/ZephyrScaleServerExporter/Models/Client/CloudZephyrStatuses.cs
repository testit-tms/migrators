using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models.Client;



public class CloudZephyrStatuses
{
    [JsonPropertyName("values")]
    public List<ZephyrStatus> Statuses { get; set; }
}
