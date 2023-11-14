using TestCollabExporter.Models;

namespace TestCollabExporter.Services;

public interface ISharedStepService
{
    Task<SharedStepData> ConvertSharedSteps(int projectId, Guid sectionId, List<Guid> attributes);
}
