using Models;
namespace QaseExporter.Services;

public interface ISharedStepService
{
    Task<Dictionary<string, SharedStep>> ConvertSharedSteps(Guid sectionId);
}
