using System.Text.Json.Serialization;

namespace SpiraTestExporter.Models;

public class SpiraTestCaseParameter
{
    [JsonPropertyName("TestCaseParameterId")]
    public int Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [JsonPropertyName("DefaultValue")]
    public string Value { get; set; }
}


