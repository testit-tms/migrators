using System.Text.Json.Serialization;

namespace SpiraTestExporter.Models;

public class SpiraPriority
{
    [JsonPropertyName("PriorityId")]
    public int Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; }

}



