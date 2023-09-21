//using AzureExporter.Models;

using AzureExporter.Models;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.TestManagement.TestPlanning.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using WorkItem = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;

namespace AzureExporter.Client;

public interface IClient
{
    Task<AzureProject> GetProject();
    Task<PagedList<TestPlan>> GetTestPlansByProjectId(Guid id);
    Task<PagedList<TestSuite>> GetTestSuitesByProjectIdAndTestPlanId(Guid projectId, int planId);
    Task<PagedList<TestCase>> GetTestCaseListByProjectIdAndTestPlanIdAndSuiteId(Guid projectId, int planId, int suiteId);
    Task<List<WorkItemReference>> GetWorkItems(string type);
    Task<WorkItem> GetWorkItemById(int id);
    Task<List<string>> GetIterations(Guid projectId);
}
