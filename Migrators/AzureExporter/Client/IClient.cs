//using AzureExporter.Models;

using AzureExporter.Models;

namespace AzureExporter.Client;

public interface IClient
{
    Task<TestPlans> GetTestPlans();
    Task<Suites> GetTestSuitesByTestPlanId(int id);
    Task<AzureTestCases> GetTestCasesByTestPlanIdTestSuiteId(int planId, int suiteId);
    Task<WorkItem> GetWorkItemById(int id);
    Task<string> GetAttachmentById(int id);
}
