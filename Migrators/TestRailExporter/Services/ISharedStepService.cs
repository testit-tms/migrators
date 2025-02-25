using TestRailExporter.Models;

namespace TestRailExporter.Services;

public interface ISharedStepService
{
    Task<SharedStepInfo> ConvertSharedSteps(int projectId, Guid sectionId);
}
