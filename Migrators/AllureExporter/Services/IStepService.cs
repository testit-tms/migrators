using Models;

namespace AllureExporter.Services;

public interface IStepService
{
    Task<List<Step>> ConvertStepsForTestCase(int testCaseId, Dictionary<string, Guid> sharedStepMap);
    Task<List<Step>> ConvertStepsForSharedStep(int sharedStepId);
}
