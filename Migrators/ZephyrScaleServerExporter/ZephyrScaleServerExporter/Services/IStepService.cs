using Models;
using ZephyrScaleServerExporter.Models.TestCases;

namespace ZephyrScaleServerExporter.Services;

public interface IStepService
{
    Task<StepsData> ConvertSteps(Guid testCaseId, ZephyrTestScript testScript, List<Iteration> iterations);
}
