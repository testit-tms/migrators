using Models;
using TestRailExporter.Models.Client;
using TestRailExporter.Models.Commons;

namespace TestRailExporter.Services;

public interface IStepService
{
    Task<List<Step>> ConvertStepsForTestCase(TestRailCase testCase, Guid testCaseId, Dictionary<int, SharedStep> sharedStepMap, AttachmentsInfo attachmentsInfo);
    Task<StepsInfo> ConvertStepsForSharedStep(TestRailSharedStep sharedStep, Guid sharedStepId);
}
