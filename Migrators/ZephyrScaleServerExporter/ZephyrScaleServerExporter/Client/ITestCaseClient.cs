using ZephyrScaleServerExporter.Models.TestCases;

namespace ZephyrScaleServerExporter.Client;

public interface ITestCaseClient
{
    Task<List<ZephyrTestCase>> GetTestCasesCoreHandler(HttpClient httpClient, string projectKey, string reqString);

    Task<List<ZephyrTestCase>> GetTestCasesCoreHandlerNewApi(HttpClient httpClient, string projectKey, string reqString);

}