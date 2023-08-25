using Models;

namespace AzureExporter.Services;

public interface ITestCaseService
{
    Task<List<TestCase>> Export();
}
