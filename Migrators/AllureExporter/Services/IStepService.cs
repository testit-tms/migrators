using Models;

namespace AllureExporter.Services;

public interface IStepService
{
    Task<List<Step>> ConvertSteps(int testCaseId);
}
