//using AzureExporter.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using AzureExporter.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace AzureExporter.Client;

public class Client : IClient
{
    private readonly ILogger<Client> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _projectName;
    private readonly string _organisationName;

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

        var organisationName = section["organisationName"];
        if (string.IsNullOrEmpty(organisationName))
        {
            throw new ArgumentException("Organisation name is not specified");
        }

        _projectName = projectName;
        _organisationName = organisationName;

        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(url);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(
                        System.Text.ASCIIEncoding.ASCII.GetBytes(
                            string.Format("{0}:{1}", "", token))));
    }

    public async Task<TestPlans> GetTestPlans()
    {
        var response = await _httpClient.GetAsync($"{_organisationName}/{_projectName}/_apis/testplan/plans?includePlanDetails=True&api-version=7.1-preview.1");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Failed to get test plans. Status code: {response.StatusCode}. Response: {await response.Content.ReadAsStringAsync()}");

            throw new Exception($"Failed to get test plans. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<TestPlans>(content);
    }

    public async Task<Suites> GetTestSuitesByTestPlanId(int id)
    {
        var response = await _httpClient.GetAsync($"{_organisationName}/{_projectName}/_apis/testplan/Plans/{id}/suites?api-version=7.1-preview.1");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Failed to get test plans. Status code: {response.StatusCode}. Response: {await response.Content.ReadAsStringAsync()}");

            throw new Exception($"Failed to get test plans. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<Suites>(content);
    }

    public async Task<AzureTestCases> GetTestCasesByTestPlanIdTestSuiteId(int planId, int suiteId)
    {
        var response = await _httpClient.GetAsync($"{_organisationName}/{_projectName}/_apis/testplan/Plans/{planId}/Suites/{suiteId}/TestCase?api-version=7.1-preview.3");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Failed to get test plans. Status code: {response.StatusCode}. Response: {await response.Content.ReadAsStringAsync()}");

            throw new Exception($"Failed to get test plans. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<AzureTestCases>(content);
    }

    public async Task<WorkItem> GetWorkItemById(int id)
    {
        var response = await _httpClient.GetAsync($"{_organisationName}/{_projectName}/_apis/wit/workitems/{id}?api-version=7.0");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Failed to get work item by id {id}. Status code: {response.StatusCode}. Response: {await response.Content.ReadAsStringAsync()}");

            throw new Exception($"Failed to get work item by id {id}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<WorkItem>(content);
    }

    public async Task<string> GetAttachmentById(int id)
    {
        var response = await _httpClient.GetAsync($"{_organisationName}/{_projectName}/_apis/wit/attachments/{id}?api-version=7.0");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Failed to get attachment by id {id}. Status code: {response.StatusCode}. Response: {await response.Content.ReadAsStringAsync()}");

            throw new Exception($"Failed to get attachment by id {id}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();

        return content;
    }
}
