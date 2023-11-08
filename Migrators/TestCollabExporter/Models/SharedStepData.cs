using Models;

namespace TestCollabExporter.Models;

public class SharedStepData
{
    public List<SharedStep> SharedSteps { get; set; }
    public Dictionary<int, Guid> SharedStepsMap { get; set; }
}
