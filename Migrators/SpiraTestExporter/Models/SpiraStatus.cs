using System.Text.Json.Serialization;

namespace SpiraTestExporter.Models;

public class SpiraStatus
{
    [JsonPropertyName("TestCaseStatusId")]
    public int Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; }
}

