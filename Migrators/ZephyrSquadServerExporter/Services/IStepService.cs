using Models;

namespace ZephyrSquadServerExporter.Services;

public interface IStepService
{
    Task<List<Step>> ConvertSteps(Guid testCaseId, string issueId);
}
