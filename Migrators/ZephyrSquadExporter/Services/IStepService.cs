using Models;

namespace ZephyrSquadExporter.Services;

public interface IStepService
{
    Task<List<Step>> ConvertSteps(Guid testCaseId, string issueId);
}
