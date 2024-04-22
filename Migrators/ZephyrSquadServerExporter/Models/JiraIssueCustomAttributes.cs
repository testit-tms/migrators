using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ZephyrSquadServerExporter.Models;

public class AllowedValue
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class JiraIssueCustomAttribute
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("hasDefaultValue")]
    public bool HasDefaultValue { get; set; }

    [JsonPropertyName("defaultValue")]
    public AllowedValue? DefaultValue { get; set; }
    
    [JsonPropertyName("allowedValues")]
    public List<AllowedValue> AllowedValues { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }
}

public class JiraIssueCustomAttributes
{
    [JsonPropertyName("values")]
    public List<JiraIssueCustomAttribute> Attributes { get; set; }

    [JsonPropertyName("total")]
    public int Count { get; set; }
}
