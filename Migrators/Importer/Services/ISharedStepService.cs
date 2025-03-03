using Importer.Models;

namespace Importer.Services;

public interface ISharedStepService
{
    Task<Dictionary<Guid, Guid>> ImportSharedSteps(Guid projectId, IEnumerable<Guid> sharedSteps,
        Dictionary<Guid, Guid> sections,
        Dictionary<Guid, TmsAttribute> attributes);
}