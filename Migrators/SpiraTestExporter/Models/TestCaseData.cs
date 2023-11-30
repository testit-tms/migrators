using Models;

namespace SpiraTestExporter.Models;

public class TestCaseData
{
    public List<TestCase> TestCases { get; set; }
    public List<SharedStep> SharedSteps { get; set; }
}
