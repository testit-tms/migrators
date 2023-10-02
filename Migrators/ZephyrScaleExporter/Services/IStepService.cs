using Models;

namespace ZephyrScaleExporter.Services;

public interface IStepService
{
    Task<List<Step>> ConvertSteps(string testCaseName, string testScript);
}
