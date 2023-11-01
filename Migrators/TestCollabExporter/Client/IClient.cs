using TestCollabExporter.Models;

namespace TestCollabExporter.Client;

public interface IClient
{
    Task<TestCollabCompanies> GetCompany();
    Task<TestCollabProject> GetProject(TestCollabCompanies companies);
    Task<List<TestCollabSuite>> GetSuites(int projectId);
    Task<List<TestCollabTestCase>> GetTestCases(int projectId, int suiteId);
    Task<List<TestCollabSharedStep>> GetSharedSteps(int projectId);
    Task<List<TestCollabCustomField>> GetCustomFields(int companyId);
}
