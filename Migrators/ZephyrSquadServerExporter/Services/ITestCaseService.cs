using Models;
using ZephyrSquadServerExporter.Models;

namespace ZephyrSquadServerExporter.Services;

public interface ITestCaseService
{
    Task<List<TestCase>> ConvertTestCases(List<ZephyrSection> allSections);
}
