using System.Text.Json.Serialization;

namespace TestRailExporter.Models.Client;

public class TestRailCaseField
{
    public const int TypeCheckbox = 5;
    public const int TypeDropdown = 6;
    public const int TypeDate = 8;
    public const int TypeSteps = 10;
    public const int TypeStepResults = 11;
    public const int TypeMultiSelect = 12;

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("type_id")]
    public int TypeId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("system_name")]
    public string SystemName { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("configs")]
    public List<TestRailCaseFieldConfig> Configs { get; set; } = [];
}

public class TestRailCaseFieldConfig
{
    [JsonPropertyName("options")]
    public TestRailCaseFieldOptions? Options { get; set; }
}

public class TestRailCaseFieldOptions
{
    [JsonPropertyName("items")]
    public string? Items { get; set; }

    [JsonPropertyName("is_required")]
    public bool IsRequired { get; set; }
}
