using System.Net.Http.Headers;
using System.Text.Json;
using AzureExporter.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.TestManagement.TestPlanning.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureExporter.Client;

public class Client : IClient
{
    private readonly ILogger<Client> _logger;
    //private readonly HttpClient _httpClient;
    private readonly ProjectHttpClient _projectClient;
    private readonly TestPlanHttpClient _testPlanClient;
    private readonly WorkHttpClient _workClient;
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
        _workClient = connection.GetClient<WorkHttpClient>();
    }

    public async Task<Guid> GetProjectId()
    {
        var projects = await _projectClient.GetProjects();
        var project = projects.FirstOrDefault(p => p.Name.Equals(_projectName, StringComparison.InvariantCultureIgnoreCase));

        if (project == null)
        {
            throw new ArgumentException($"Project {_projectName} is not found");
        }

        return project.Id;
    }

    public async Task<PagedList<TestPlan>> GetTestPlansByProjectId(Guid id)
    {
        var testPlans = await _testPlanClient.GetTestPlansAsync(project: id);

        return testPlans;
    }

    public async Task<PagedList<TestSuite>> GetTestSuitesByProjectIdAndTestPlanId(Guid projectId, int planId)
    {
        var testSuites = await _testPlanClient.GetTestSuitesForPlanAsync(project: projectId, planId: planId);

        return testSuites;
    }

    public async Task<PagedList<TestCase>> GetTestCaseListByProjectIdAndTestPlanIdAndSuiteId(Guid projectId, int planId, int suiteId)
    {
        var testCases = await _testPlanClient.GetTestCaseListAsync(project: projectId, planId: planId, suiteId: suiteId);

        return testCases;
    }

    //public async Task<AzureTestPlans> GetTestPlans()
    //{
    //    using (HttpResponseMessage response = _httpClient.GetAsync(
    //                $"{_organisationName}/{_projectName}/_apis/test/plans?api-version=5.0").Result)
    //    {
    //        if (!response.IsSuccessStatusCode)
    //        {
    //            _logger.LogError($"Failed to get test plans. Status code: {response.StatusCode}. Response: {await response.Content.ReadAsStringAsync()}");

    //            throw new Exception($"Failed to get test plans. Status code: {response.StatusCode}");
    //        }

    //        var content = await response.Content.ReadAsStringAsync();

    //        return JsonSerializer.Deserialize<AzureTestPlans>(content);
    //    }
    //}

    //public async Task<AzureSuites> GetTestSuitesByTestPlanId(int id)
    //{
    //    using (HttpResponseMessage response = _httpClient.GetAsync(
    //        $"{_organisationName}/{_projectName}/_apis/test/plans/{id}/suites?api-version=5.0").Result)
    //    {
    //        if (!response.IsSuccessStatusCode)
    //        {
    //            _logger.LogError($"Failed to get test suites. Status code: {response.StatusCode}. Response: {await response.Content.ReadAsStringAsync()}");

    //            throw new Exception($"Failed to get test suites. Status code: {response.StatusCode}");
    //        }

    //        var content = await response.Content.ReadAsStringAsync();

    //        return JsonSerializer.Deserialize<AzureSuites>(content);
    //    }
    //}

    //public async Task<AzureTestPoints> GetTestCasesByTestPlanIdTestSuiteId(int planId, int suiteId)
    //{
    //    using (HttpResponseMessage response = _httpClient.GetAsync(
    //        $"{_organisationName}/{_projectName}/_apis/test/Plans/{planId}/suites/{suiteId}/testcases?api-version=5.0").Result)
    //    {
    //        if (!response.IsSuccessStatusCode)
    //        {
    //            _logger.LogError($"Failed to get test cases. Status code: {response.StatusCode}. Response: {await response.Content.ReadAsStringAsync()}");

    //            throw new Exception($"Failed to get test cases. Status code: {response.StatusCode}");
    //        }

    //        var content = await response.Content.ReadAsStringAsync();

    //        return JsonSerializer.Deserialize<AzureTestPoints>(content);
    //    }
    //}

    //public async Task<AzureWorkItem> GetWorkItemById(string id)
    //{
    //    using (HttpResponseMessage response = _httpClient.GetAsync(
    //        $"{_organisationName}/{_projectName}/_apis/wit/workitems/{id}?api-version=5.0").Result)
    //    {
    //        if (!response.IsSuccessStatusCode)
    //        {
    //            _logger.LogError($"Failed to get work item by id {id}. Status code: {response.StatusCode}. Response: {await response.Content.ReadAsStringAsync()}");

    //            throw new Exception($"Failed to get work item by id {id}. Status code: {response.StatusCode}");
    //        }

    //        var content = await response.Content.ReadAsStringAsync();

    //        return JsonSerializer.Deserialize<AzureWorkItem>(content);
    //    }
    //}

    //public async Task<string> GetAttachmentById(int id)
    //{
    //    using (HttpResponseMessage response = _httpClient.GetAsync(
    //        $"{_organisationName}/{_projectName}/_apis/wit/attachments/{id}?api-version=7.0").Result)
    //    {
    //        if (!response.IsSuccessStatusCode)
    //        {
    //            _logger.LogError($"Failed to get attachment by id {id}. Status code: {response.StatusCode}. Response: {await response.Content.ReadAsStringAsync()}");

    //            throw new Exception($"Failed to get attachment by id {id}. Status code: {response.StatusCode}");
    //        }

    //        var content = await response.Content.ReadAsStringAsync();

    //        return content;
    //    }
    //}
}
