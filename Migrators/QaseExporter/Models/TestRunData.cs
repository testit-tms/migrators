using Models;

namespace QaseExporter.Models;

public class TestRunData
{
    public List<TestRun> TestRuns { get; set; } = new();
    public List<TestPlan> TestPlans { get; set; } = new();
}
