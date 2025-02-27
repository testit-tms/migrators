using TestRailExporter.Models.Commons;

namespace TestRailExporter.Services;

public interface ISharedStepService
{
    Task<SharedStepInfo> ConvertSharedSteps(int projectId, Guid sectionId);
}
