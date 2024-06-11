using Models;
using ZephyrScaleServerExporter.Models;

namespace ZephyrScaleServerExporter.Services;

public interface IStepService
{
    Task<List<Step>> ConvertSteps(Guid testCaseId, ZephyrTestScript testScript);
}
