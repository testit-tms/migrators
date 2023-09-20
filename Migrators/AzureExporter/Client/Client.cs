using System.Net.Http.Headers;
using System.Text.Json;
using AzureExporter.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.TestManagement.TestPlanning.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using WorkItem = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;

namespace AzureExporter.Client;

// public class WorkItemsType
// {
//     private WorkItemsType(string value) { Value = value; }
//
//     public string Value { get; private set; }
//
//     public static WorkItemsType SharedSteps { get { return new WorkItemsType("Shared Steps"); } }
//     public static WorkItemsType TestCases { get { return new WorkItemsType("Test Case"); } }
//
//     public override string ToString()
//     {
//         return Value;
//     }
// }

public class Client : IClient
{
    private readonly ILogger<Client> _logger;

    //private readonly HttpClient _httpClient;
    private readonly ProjectHttpClient _projectClient;
    private readonly TestPlanHttpClient _testPlanClient;
    private readonly WorkItemTrackingHttpClient _workItemTrackingClient;
    private readonly string _projectName;

    public Client(ILogger<Client> logger, IConfiguration configuration)
    {
        _logger = logger;

        var section = configuration.GetSection("azure");
        var url = section["url"];

        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException("Url is not specified");
        }

        var token = section["token"];

        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentException("Private token is not specified");
        }

        var projectName = section["projectName"];

        if (string.IsNullOrEmpty(projectName))
        {
            throw new ArgumentException("Project name is not specified");
        }

        _projectName = projectName;

        //_httpClient = new HttpClient();
        //_httpClient.BaseAddress = new Uri(url);
        //_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
        //            Convert.ToBase64String(
        //                System.Text.ASCIIEncoding.ASCII.GetBytes(
        //                    string.Format("{0}:{1}", "", token))));

        var connection = new VssConnection(new Uri(url), new VssBasicCredential(string.Empty, token));
        _projectClient = connection.GetClient<ProjectHttpClient>();
        _testPlanClient = connection.GetClient<TestPlanHttpClient>();
        _workItemTrackingClient = connection.GetClient<WorkItemTrackingHttpClient>();
    }

    public async Task<AzureProject> GetProject()
    {
        var projects = _projectClient.GetProjects().Result;
        var project = projects.FirstOrDefault(p =>
                p.Name.Equals(_projectName, StringComparison.InvariantCultureIgnoreCase));

        if (project == null)
        {
            throw new ArgumentException($"Project {_projectName} is not found");
        }

        return new AzureProject
        {
            Id = project.Id,
            Name = project.Name
        };
    }

    public async Task<PagedList<TestPlan>> GetTestPlansByProjectId(Guid id)
    {
        var testPlans = _testPlanClient.GetTestPlansAsync(project: id).Result;

        return testPlans;
    }

    public async Task<PagedList<TestSuite>> GetTestSuitesByProjectIdAndTestPlanId(Guid projectId, int planId)
    {
        var testSuites = _testPlanClient.GetTestSuitesForPlanAsync(project: projectId, planId: planId).Result;

        return testSuites;
    }

    public async Task<PagedList<TestCase>> GetTestCaseListByProjectIdAndTestPlanIdAndSuiteId(Guid projectId, int planId,
        int suiteId)
    {
        var testCases = _testPlanClient.GetTestCaseListAsync(project: projectId, planId: planId, suiteId: suiteId)
            .Result;

        return testCases;
    }

    public async Task<List<WorkItemReference>> GetWorkItems(string workItemType)
    {
        Wiql wiql = new Wiql();
        wiql.Query = $"SELECT [System.Id],[System.WorkItemType],[System.Title],[System.AssignedTo],[System.State],[System.Tags] " +
                     $"FROM WorkItems " +
                     $"WHERE [System.TeamProject] = '{_projectName}' " +
                     $"AND [System.WorkItemType] = '{workItemType}'";

        var queryResult = _workItemTrackingClient.QueryByWiqlAsync(wiql).Result;

        return queryResult.WorkItems.ToList();
    }

    public async Task<WorkItem> GetWorkItemById(int id)
    {
        var workItem = _workItemTrackingClient.GetWorkItemAsync(_projectName, id).Result;

        return workItem;
    }
}
