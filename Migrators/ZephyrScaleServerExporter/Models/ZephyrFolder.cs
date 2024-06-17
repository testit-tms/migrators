using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models;

public class ZephyrFolder
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("parentId")]
    public int? ParentId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class ZephyrFolders
{
    [JsonPropertyName("values")]
    public List<ZephyrFolder> Folders { get; set; }
}

