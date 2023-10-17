using Models;
using ZephyrSquadExporter.Models;

namespace ZephyrSquadExporter.Services;

public interface ITestCaseService
{
    Task<List<TestCase>> ConvertTestCases(Dictionary<string, ZephyrSection> sectionMap);
}
