using System.Text.Json.Serialization;

namespace PractiTestExporter.Models;

public class CustomFieldAttributes
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("field-format")]
    public string FieldFormat { get; set; }

    [JsonPropertyName("project-id")]
    public int ProjectId { get; set; }
}

public class ListCustomFieldAttributes : CustomFieldAttributes
{
    [JsonPropertyName("possible-values")]
    public List<string> PossibleValues { get; set; }
}

public class PractiTestCustomField
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("attributes")]
    public CustomFieldAttributes Attributes { get; set; }
}

public class ListPractiTestCustomField
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("attributes")]
    public ListCustomFieldAttributes Attributes { get; set; }
}

public class PractiTestCustomFields
{
    [JsonPropertyName("data")]
    public List<PractiTestCustomField> Data { get; set; }
}

public class SinglePractiTestCustomField
{
    [JsonPropertyName("data")]
    public ListPractiTestCustomField Data { get; set; }
}
