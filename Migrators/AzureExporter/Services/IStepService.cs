using Models;

namespace AzureExporter.Services;

public interface IStepService
{
    Task<List<Step>> ConvertSteps(string steps, Dictionary<int, Guid> sharedStepMap);
}
