using Importer.Models;
using Models;

namespace Importer.Services;

public interface ISharedStepService
{
    Task<Dictionary<Guid, Guid>> ImportSharedSteps(IEnumerable<Guid> sharedSteps, Dictionary<Guid, Guid> sections,
        Dictionary<Guid, TmsAttribute> attributes);
}
