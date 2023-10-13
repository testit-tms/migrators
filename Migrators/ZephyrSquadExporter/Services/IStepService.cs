using Models;

namespace ZephyrSquadExporter.Services;

public interface IStepService
{
    Task<List<Step>> ConvertSteps(string issueId);
}
