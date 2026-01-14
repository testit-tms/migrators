using System.Text.Json.Serialization;

namespace QaseExporter.Models;

public class QaseTestCase
{

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = null!;

    [JsonPropertyName("preconditions")]
    public string Preconditions { get; set; } = null!;

    [JsonPropertyName("postconditions")]
    public string Postconditions { get; set; } = null!;

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("isManual")]
    public bool isManual { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("suite_id")]
    public int SuiteId { get; set; }

    //[JsonPropertyName("links")]
    //public List<QaseLink> Links { get; set; }

    [JsonPropertyName("steps")]
    public List<QaseStep> Steps { get; set; } = new();

    [JsonPropertyName("custom_fields")]
    public List<QaseCustomFieldValues> CustomFields { get; set; } = new();

    [JsonPropertyName("params")]
    public object Parameters { get; set; } = null!;

    [JsonPropertyName("steps_type")]
    public string StepsType { get; set; } = null!;

    [JsonPropertyName("attachments")]
    public List<QaseAttachment> Attachments { get; set; } = new();

    [JsonPropertyName("tags")]
    public List<QaseTag> Tags { get; set; } = new();

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("layer")]
    public int Layer { get; set; }

    [JsonPropertyName("is_flaky")]
    public int IsFlaky { get; set; }

    [JsonPropertyName("severity")]
    public int Severity { get; set; }

    [JsonPropertyName("behavior")]
    public int Behavior { get; set; }

    [JsonPropertyName("isToBeAutomated")]
    public bool ToBeAutomated { get; set; }

    [JsonPropertyName("author_id")]
    public int AuthorId { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreateAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdateAt { get; set; }
}

public class QaseCustomFieldValues
{
    [JsonPropertyName("id")]
    public int FieldId { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; } = null!;
}

public class QaseCases
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("filtered")]
    public int Filtered { get; set; }

    [JsonPropertyName("entities")]
    public List<QaseTestCase> Cases { get; set; } = new();
}

public class QaseCasesData
{
    [JsonPropertyName("result")]
    public QaseCases CasesData { get; set; } = null!;
}
