using Models;
using Attribute = Models.Attribute;

namespace XRayExporter.Models;

public class TestCaseData
{
    public List<TestCase> TestCases { get; set; }
    public List<SharedStep> SharedSteps { get; set; }
    public List<Attribute> Attributes { get; set; }
}
