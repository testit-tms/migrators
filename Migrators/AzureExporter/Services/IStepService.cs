using Models;

namespace AzureExporter.Services;

public interface IStepService
{
    abstract List<Step> ConvertSteps(string steps);
}
