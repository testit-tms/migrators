//using AzureExporter.Models;
using Microsoft.VisualStudio.Services.TestManagement.TestPlanning.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureExporter.Client;

public interface IClient
{
    Task<Guid> GetProjectId();
    Task<PagedList<TestPlan>> GetTestPlansByProjectId(Guid id);
    Task<PagedList<TestSuite>> GetTestSuitesByProjectIdAndTestPlanId(Guid projectId, int planId);
    Task<PagedList<TestCase>> GetTestCaseListByProjectIdAndTestPlanIdAndSuiteId(Guid projectId, int planId, int suiteId);

    //Task<AzureTestPlans> GetTestPlans();
    //Task<AzureSuites> GetTestSuitesByTestPlanId(int id);
    //Task<AzureTestPoints> GetTestCasesByTestPlanIdTestSuiteId(int planId, int suiteId);
    //Task<AzureWorkItem> GetWorkItemById(string id);
    //Task<string> GetAttachmentById(int id);
}
