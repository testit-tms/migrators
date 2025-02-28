using Models;
using TestRailExporter.Models.Client;
using TestRailExporter.Models.Commons;

namespace TestRailExporter.Services;

public interface IStepService
{
    Task<List<Step>> ConvertStepsForTestCase(TestRailCase testCase, Guid testCaseId, Dictionary<int, SharedStep> sharedStepMap, Dictionary<int, string> attachmentsMap);
    Task<StepsInfo> ConvertStepsForSharedStep(TestRailSharedStep sharedStep, Guid sharedStepId);
}
