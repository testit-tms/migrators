using System.Text.Json.Serialization;

namespace TestCollabExporter.Models;

public class TestCollabCompanies
{
    [JsonPropertyName("companies")]
    public List<TestCollabCompany> Companies { get; set; }
}

public class TestCollabCompany
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
}

