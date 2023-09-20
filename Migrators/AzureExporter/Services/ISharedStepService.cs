using Models;

namespace AzureExporter.Services;

public interface ISharedStepService
{
    Task<Dictionary<int, SharedStep>> ConvertSharedSteps(Guid projectId, Guid sectionId);
}
