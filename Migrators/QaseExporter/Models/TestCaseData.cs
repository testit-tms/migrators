using Models;

namespace QaseExporter.Models;

public class TestCaseData
{
    public List<TestCase> TestCases { get; set; } = new();
    public Dictionary<int, Guid> TestCaseMap { get; set; } = new();
}
