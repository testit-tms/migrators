using Models;

namespace QaseExporter.Models;

public class TestResultData
{
    public List<TestResult> ManualTestResults { get; set; } = new();
    public List<TestResult> AutoTestResults { get; set; } = new();
}
