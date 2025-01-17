using Models;

namespace AllureExporter.Services;

public interface IStepService
{
    Task<List<Step>> ConvertStepsForTestCase(long testCaseId, Dictionary<string, Guid> sharedStepMap);
    Task<List<Step>> ConvertStepsForSharedStep(long sharedStepId);
}
