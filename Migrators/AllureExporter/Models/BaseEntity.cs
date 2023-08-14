using System.Text.Json.Serialization;

namespace AllureExporter.Models;

public class BaseEntity
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class BaseEntities
{
    [JsonPropertyName("content")]
    public BaseEntity[] Content { get; set; }
}




