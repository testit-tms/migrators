using TestCollabExporter.Models;

namespace TestCollabExporter.Services;

public interface ISharedStepService
{
    Task<SharedStepData> GetSharedSteps(int projectId, Guid sectionId);
}
