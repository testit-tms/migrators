using Models;
using TestRailExporter.Models.Commons;

namespace TestRailExporter.Services;

public interface ITestCaseService
{
    Task<List<TestCase>> ConvertTestCases(int projectId, Dictionary<int, SharedStep> sharedStepMap,  SectionInfo sectionInfo);
}
