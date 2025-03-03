using Models;

namespace Importer.Services;

public interface ISectionService
{
    Task<Dictionary<Guid, Guid>> ImportSections(Guid projectId, IEnumerable<Section> sections);
}