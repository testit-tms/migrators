using QaseExporter.Models;

namespace QaseExporter.Client;

public interface IClient
{
    Task<QaseProject> GetProject();
    Task<List<QaseSuite>> GetSuites();
    Task<List<QaseTestCase>> GetTestCasesBySuiteId(int suiteId);
    Task<List<QaseSharedStep>> GetSharedSteps();
    Task<List<QaseCustomField>> GetCustomFields();
    Task<List<QaseSystemField>> GetSystemFields();
    Task<byte[]> DownloadAttachment(string url);
}
