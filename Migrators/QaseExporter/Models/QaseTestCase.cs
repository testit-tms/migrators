using System.Text.Json.Serialization;

namespace QaseExporter.Models;

public class QaseTestCase
{

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("preconditions")]
    public string Preconditions { get; set; }

    [JsonPropertyName("postconditions")]
    public string Postconditions { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("automation")]
    public int AutomationStatus { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("suite_id")]
    public int SuiteId { get; set; }

    //[JsonPropertyName("links")]
    //public List<QaseLink> Links { get; set; }

    [JsonPropertyName("steps")]
    public List<QaseStep> Steps { get; set; }

    [JsonPropertyName("custom_fields")]
    public List<QaseCustomFieldValues> CustomFields { get; set; }

    [JsonPropertyName("params")]
    public object Parameters { get; set; }

    [JsonPropertyName("steps_type")]
    public string StepsType { get; set; }

    [JsonPropertyName("attachments")]
    public List<QaseAttachment> Attachments { get; set; }

    [JsonPropertyName("tags")]
    public List<QaseTag> Tags { get; set; }

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
}

public class QaseCustomFieldValues
{
    [JsonPropertyName("id")]
    public int FieldId { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }
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
    public List<QaseTestCase> Cases { get; set; }
}

public class QaseCasesData
{
    [JsonPropertyName("result")]
    public QaseCases CasesData { get; set; }
}
