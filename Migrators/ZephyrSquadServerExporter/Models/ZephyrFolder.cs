using System.Text.Json.Serialization;

namespace ZephyrSquadServerExporter.Models;

public class ZephyrFolder
{
    [JsonPropertyName("folderName")]
    public string Name { get; set; }

    [JsonPropertyName("folderId")]
    public int Id { get; set; }
}
