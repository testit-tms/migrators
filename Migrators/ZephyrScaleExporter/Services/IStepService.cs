using Models;

namespace ZephyrScaleExporter.Services;

public interface IStepService
{
    Task<List<Step>> ConvertSteps(Guid testCaseId, string testCaseName, string testScript);
}
