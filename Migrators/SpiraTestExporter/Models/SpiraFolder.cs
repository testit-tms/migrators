using System.Text.Json.Serialization;

namespace SpiraTestExporter.Models;

public class SpiraFolder
{
    [JsonPropertyName("TestCaseFolderId")]
    public int Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [JsonPropertyName("ParentTestCaseFolderId")]
    public int? ParentId { get; set; }
}


