using Models;
using Attribute = Models.Attribute;

namespace AllureExporter.Services;

public interface ISharedStepService
{
    Task<Dictionary<long, SharedStep>> ConvertSharedSteps(long projectId, Guid sectionId, List<Attribute> attributes);
}
