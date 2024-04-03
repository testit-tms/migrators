using Models;
using Attribute = Models.Attribute;

namespace AllureExporter.Services;

public interface ISharedStepService
{
    Task<Dictionary<int, SharedStep>> ConvertSharedSteps(int projectId, Guid sectionId, List<Attribute> attributes);
}
