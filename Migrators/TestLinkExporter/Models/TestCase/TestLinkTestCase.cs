using TestLinkExporter.Models.Step;

namespace TestLinkExporter.Models.TestCase;

public class TestLinkTestCase
{
    public int ExecutionType { get; set; }
    public string ExternalId { get; set; }
    public int Id { get; set; }
    public int Importance { get; set; }
    public bool IsOpen { get; set; }
    public string Layout { get; set; }
    public string Name { get; set; }
    public string Preconditions { get; set; }
    public int Status { get; set; }
    public List<TestLinkStep> Steps { get; set; }
    public string Summary { get; set; }
    public int TestSuiteId { get; set; }
}
