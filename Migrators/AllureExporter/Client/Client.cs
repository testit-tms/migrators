using System.Net.Http.Headers;
using System.Text.Json;
using AllureExporter.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AllureExporter.Client;

public class Client : IClient
{
    private readonly ILogger<Client> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _projectName;

    public Client(ILogger<Client> logger, IConfiguration configuration)
    {
        _logger = logger;

        var tmsSection = configuration.GetSection("allure");
        var url = tmsSection["url"];
        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException("TMS url is not specified");
        }

        var token = tmsSection["token"];
        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentException("TMS private token is not specified");
        }

        var projectName = tmsSection["projectName"];
        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentException("TMS private token is not specified");
        }

        _projectName = projectName;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(url);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Api-Token", token);
    }

    public async Task<int> GetProjectId()
    {
        _logger.LogInformation("Getting project id with name {Name}", _projectName);

        var response = await _httpClient.GetAsync("api/rs/project");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get project id. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get project id. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var projects = JsonSerializer.Deserialize<BaseEntities>(content);
        var project = projects?.Content.FirstOrDefault(p =>
            string.Equals(p.Name, _projectName, StringComparison.InvariantCultureIgnoreCase));

        if (project != null) return project.Id;

        _logger.LogError("Project not found");

        throw new Exception("Project not found");
    }


    public async Task<List<int>> GetTestCaseIdsFromMainSuite(int projectId)
    {
        _logger.LogInformation("Getting test case ids from main suite");

        var response = await _httpClient.GetAsync($"api/rs/testcasetree/leaf?projectId={projectId}&treeId=2");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get test case ids from main suite. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get test case ids from main suite. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var testCases = JsonSerializer.Deserialize<BaseEntities>(content);

        return testCases is { Content.Length: 0 } ? new List<int>() : testCases.Content.Select(t => t.Id).ToList();
    }

    public async Task<List<int>> GetTestCaseIdsFromSuite(int projectId, int suiteId)
    {
        _logger.LogInformation("Getting test case ids from suite {SuiteId}", suiteId);

        var response =
            await _httpClient.GetAsync($"api/rs/testcasetree/leaf?projectId={projectId}&treeId=2&path={suiteId}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get test case ids from suite {SuiteId}. Status code: {StatusCode}. Response: {Response}",
                suiteId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get test case ids from suite {suiteId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var testCases = JsonSerializer.Deserialize<BaseEntities>(content);

        return testCases is { Content.Length: 0 } ? new List<int>() : testCases.Content.Select(t => t.Id).ToList();
    }

    public async Task<AllureTestCase> GetTestCaseById(int testCaseId)
    {
        _logger.LogInformation("Getting test case with id {TestCaseId}", testCaseId);

        var response = await _httpClient.GetAsync($"api/rs/testcase/{testCaseId}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get test case with id {TestCaseId}. Status code: {StatusCode}. Response: {Response}",
                testCaseId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get test case with id {testCaseId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AllureTestCase>(content);
    }

    public async Task<List<AllureStep>> GetSteps(int testCaseId)
    {
        _logger.LogInformation("Getting steps for test case with id {TestCaseId}", testCaseId);

        var response = await _httpClient.GetAsync($"api/rs/testcase/{testCaseId}/scenario");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get steps for test case with id {TestCaseId}. Status code: {StatusCode}. Response: {Response}",
                testCaseId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get steps for test case with id {testCaseId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<AllureStep>>(content);
    }

    public async Task<List<AllureAttachment>> GetAttachments(int testCaseId)
    {
        _logger.LogInformation("Getting attachments for test case with id {TestCaseId}", testCaseId);

        var response = await _httpClient.GetAsync($"api/rs/testcase/attachment?testCaseId={testCaseId}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get attachments for test case with id {TestCaseId}. Status code: {StatusCode}. Response: {Response}",
                testCaseId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get attachments for test case with id {testCaseId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<AllureAttachment>>(content);
    }

    public async Task<List<AllureLink>> GetLinks(int testCaseId)
    {
        _logger.LogInformation("Getting links for test case with id {TestCaseId}", testCaseId);

        var response = await _httpClient.GetAsync($"api/rs/testcase/{testCaseId}/issue");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get links for test case with id {TestCaseId}. Status code: {StatusCode}. Response: {Response}",
                testCaseId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get links for test case with id {testCaseId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<AllureLink>>(content);
    }
}
