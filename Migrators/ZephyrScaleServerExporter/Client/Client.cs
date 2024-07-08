using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZephyrScaleServerExporter.Models;

namespace ZephyrScaleServerExporter.Client;

public class Client : IClient
{
    private readonly ILogger<Client> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _projectKey;

    public Client(ILogger<Client> logger, IConfiguration configuration)
    {
        _logger = logger;

        var section = configuration.GetSection("zephyr");
        var url = section["url"];
        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException("Url is not specified");
        }

        var projectKey = section["projectKey"];
        if (string.IsNullOrEmpty(projectKey))
        {
            throw new ArgumentException("Project key is not specified");
        }

        var token = section["token"];
        var login = section["login"];
        var password = section["password"];

        _projectKey = projectKey;

        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(url);

        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
        }
        else if (!string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(password))
        {
            var basicAuthenticationValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{login}:{password}"));

            _httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + basicAuthenticationValue);
        }
        else
        {
            throw new ArgumentException("Token or login/password is not specified");
        }
    }

    public async Task<ZephyrProject> GetProject()
    {
        _logger.LogInformation("Getting project by key {ProjectKey}", _projectKey);

        var response = await _httpClient.GetAsync($"/rest/api/2/project/{_projectKey}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get project by key {ProjectKey}. Status code: {StatusCode}. Response: {Response}",
                _projectKey, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get project by key {_projectKey}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var project = JsonSerializer.Deserialize<ZephyrProject>(content);

        _logger.LogDebug("Found project {@ProjectId}", project);

        return project;
    }

    public async Task<List<ZephyrTestCase>> GetTestCases()
    {
        _logger.LogInformation("Getting test cases by project key {Key}", _projectKey);
        var allTestCases = new List<ZephyrTestCase>();
        var page = 0;

        do
        {
            var response = await _httpClient.GetAsync($"/rest/atm/1.0/testcase/search?maxResults=100&startAt={page}&query=projectKey = \"{_projectKey}\"");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get test cases by project key {Key}. Status code: {StatusCode}. Response: {Response}",
                    _projectKey, response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception($"Failed to get test cases by project key {_projectKey}. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var testCases = JsonSerializer.Deserialize<List<ZephyrTestCase>>(content);

            if (testCases.Any())
            {
                _logger.LogDebug("Got test cases {@TestCases} from {Page} page", testCases, page);
                allTestCases.AddRange(testCases);
                page++;
            }
            else
            {
                page = -1;
            }
        } while (page >= 0);

        return allTestCases;
    }

    public async Task<ZephyrTestCase> GetTestCase(string testCaseKey)
    {
        _logger.LogInformation("Getting test case by key {Key}", testCaseKey);

        var response = await _httpClient.GetAsync($"/rest/atm/1.0/testcase/{testCaseKey}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get test case by key {Key}. Status code: {StatusCode}. Response: {Response}",
                testCaseKey, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get test case by key {testCaseKey}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var testCase = JsonSerializer.Deserialize<ZephyrTestCase>(content);

        _logger.LogDebug("Got test case {@TestCase}", testCase);

        return testCase;
    }

    public async Task<List<JiraComponent>> GetComponents(string projectKey)
    {
        _logger.LogInformation("Getting components by project key {Key}", projectKey);

        var response = await _httpClient.GetAsync($"/rest/api/2/project/{projectKey}/components");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get components by project key {Key}. Status code: {StatusCode}. Response: {Response}",
                projectKey, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get components by project key {projectKey}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var components = JsonSerializer.Deserialize<List<JiraComponent>>(content);

        _logger.LogDebug("Got components: {@Issue}", components);

        return components;
    }

    public async Task<JiraIssue> GetIssueById(string issueId)
    {
        _logger.LogInformation("Getting issue by id {IssueId}", issueId);

        var response = await _httpClient.GetAsync($"/rest/api/2/issue/{issueId}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get issue by id {IssueId}. Status code: {StatusCode}. Response: {Response}",
                issueId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get issue by id {issueId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var issue = JsonSerializer.Deserialize<JiraIssue>(content);

        _logger.LogDebug("Got issue: {@Issue}", issue);

        return issue;
    }

    public async Task<List<ZephyrAttachment>> GetAttachmentsForTestCase(string testCaseKey)
    {
        _logger.LogInformation("Getting attachments by test case key {Key}", testCaseKey);

        var response = await _httpClient.GetAsync($"/rest/atm/1.0/testcase/{testCaseKey}/attachments");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get attachments by test case key {Key}. Status code: {StatusCode}. Response: {Response}",
                testCaseKey, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get attachments by test case key {testCaseKey}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var attachments = JsonSerializer.Deserialize<List<ZephyrAttachment>>(content);

        _logger.LogDebug("Got attachments {@Attachments}", attachments);

        return attachments;
    }

    public async Task<byte[]> DownloadAttachment(string url)
    {
        return await _httpClient.GetByteArrayAsync(url);
    }
}
