using System.Text.Json.Serialization;

namespace AllureExporter.Models;


public class AllureTestCases
{
    [JsonPropertyName("content")]
    public List<AllureTestCaseBase> Content { get; set; } = new();

    [JsonPropertyName("totalPages")]
    public long TotalPages { get; set; }
}

public class AllureTestCaseBase
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("automated")]
    public bool Automated { get; set; }
}

public class AllureTestCase : AllureTestCaseBase
{
    [JsonPropertyName("projectId")]
    public long ProjectId { get; set; }

    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }

    [JsonPropertyName("external")]
    public bool External { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("descriptionHtml")]
    public string DescriptionHtml { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<Tag> Tags { get; set; } = new();

    [JsonPropertyName("links")]
    public List<TestCaseLink> Links { get; set; } = new();

    [JsonPropertyName("status")]
    public Status? Status { get; set; }

    [JsonPropertyName("testLayer")]
    public TestLayer? Layer { get; set; }

    [JsonPropertyName("workflow")]
    public Workflow? Workflow { get; set; }

    [JsonPropertyName("precondition")]
    public string? Precondition { get; set; }
}

public class TestCaseLink
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class Workflow
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class Tag
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class Status
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class TestLayer
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
