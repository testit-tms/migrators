using System.Text.Json.Serialization;

namespace XRayExporter.Models;

public class XRayFolders
{
    [JsonPropertyName("folders")]
    public List<XrayFolder> Folders { get; set; }
}

public class XrayFolder
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("folders")]
    public List<XrayFolder> Folders { get; set; }
}

