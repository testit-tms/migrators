using System.Text.Json.Serialization;

namespace AllureExporter.Models;


public class AllureTestCases
{
    [JsonPropertyName("content")]
    public List<AllureTestCaseBase> Content { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }
}

public class AllureTestCaseBase
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("automated")]
    public bool Automated { get; set; }
}

public class AllureTestCase : AllureTestCaseBase
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("tags")]
    public List<Tags> Tags { get; set; }

    [JsonPropertyName("status")]
    public Status Status { get; set; }

    [JsonPropertyName("testLayer")]
    public TestLayer? Layer { get; set; }

    [JsonPropertyName("precondition")]
    public string? Precondition { get; set; }
}

public class Tags
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class Status
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class TestLayer
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}
