using System.Text.Json.Serialization;

namespace SpiraTestExporter.Models;

public class SpiraTest
{
    [JsonPropertyName("TestCaseId")]
    public int TestCaseId { get; set; }

    [JsonPropertyName("TestCaseFolderId")]
    public int? PriorityId { get; set; }

    [JsonPropertyName("TestCaseStatusId")]
    public int StatusId { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [JsonPropertyName("Description")]
    public string Description { get; set; }

    [JsonPropertyName("AuthorName")]
    public string AuthorName { get; set; }

    [JsonPropertyName("TestCaseFolderId")]
    public int? FolderId { get; set; }
}



