using Models;

namespace AzureExporter.Services;

public interface IStepService
{
    List<Step> ConvertSteps(string steps, Dictionary<int, Guid> sharedStepMap);
}
