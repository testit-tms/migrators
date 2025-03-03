using System.Text.Json.Serialization;

namespace AllureExporter.Models.Project;

public class BaseEntity
{
    [JsonPropertyName("id")] public long Id { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
}

public class BaseEntities
{
    [JsonPropertyName("content")] public List<BaseEntity> Content { get; set; } = new();
}
