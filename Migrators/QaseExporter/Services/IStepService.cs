using Models;
using QaseExporter.Models;

namespace QaseExporter.Services;

public interface IStepService
{
    Task<List<Step>> ConvertSteps(List<QaseStep> testLinkSteps, Dictionary<string, SharedStep> sharedSteps, Guid testCaseId);
    Task<List<Step>> ConvertConditionSteps(string conditions, Guid testCaseId);
}
