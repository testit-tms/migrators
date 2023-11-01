using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TestCollabExporter.Models;

namespace TestCollabExporter.Client;

public class Client : IClient
{
    private readonly ILogger<Client> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private readonly string _projectName;

    public Client(ILogger<Client> logger, IConfiguration configuration)
    {
        _logger = logger;
        _logger = logger;

        var section = configuration.GetSection("testCollab");
        var url = section["url"];
        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException("Url is not specified");
        }

        var token = section["token"];
        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentException("Token is not specified");
        }

        var projectName = section["projectName"];
        if (string.IsNullOrEmpty(projectName))
        {
            throw new ArgumentException("Project name is not specified");
        }

        _projectName = projectName;
        _token = token;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(url);
    }

    public async Task<TestCollabCompanies> GetCompany()
    {
        _logger.LogInformation("Getting companies");

        var response = await _httpClient.GetAsync($"users/me?token={_token}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get companies. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get companies. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var companies = JsonSerializer.Deserialize<TestCollabCompanies>(content);

        _logger.LogDebug("Found {Count} companies", companies!.Companies.Count);

        return companies;
    }

    public async Task<TestCollabProject> GetProject(TestCollabCompanies companies)
    {
        _logger.LogInformation("Getting project {ProjectName}", _projectName);

        foreach (var testCollabCompany in companies.Companies)
        {
            _logger.LogDebug("Getting projects for company {CompanyId}", testCollabCompany.Id);

            var response = await _httpClient.GetAsync($"projects?company={testCollabCompany.Id}&token={_token}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get projects for company {CompanyId}. Status code: {StatusCode}. Response: {Response}",
                    testCollabCompany.Id, response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception(
                    $"Failed to get get projects for company {testCollabCompany.Id}. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var projects = JsonSerializer.Deserialize<List<TestCollabProject>>(content);

            var project = projects!
                .FirstOrDefault(p => p.Name.Equals(_projectName, StringComparison.InvariantCultureIgnoreCase));

            if (project != null) return project;
        }

        _logger.LogError("Project not found");

        throw new ApplicationException("Project not found");
    }

    public async Task<List<TestCollabSuite>> GetSuites(int projectId)
    {
        _logger.LogInformation("Getting suites for project {ProjectId}", projectId);

        var response = await _httpClient.GetAsync($"suites?project=12123&token={_token}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get suites for project {ProjectId}. Status code: {StatusCode}. Response: {Response}",
                projectId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get suites for project {projectId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var suites = JsonSerializer.Deserialize<List<TestCollabSuite>>(content);

        _logger.LogDebug("Found {Count} suites", suites!.Count);

        return suites;
    }

    public async Task<List<TestCollabTestCase>> GetTestCases(int projectId, int suiteId)
    {
        _logger.LogInformation("Getting test cases for project {ProjectId} and suite {SuiteId}", projectId, suiteId);

        var response = await _httpClient.GetAsync($"testcases?project={projectId}&suite.id={suiteId}&token={_token}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get test cases for project {ProjectId} and suite {SuiteId}. Status code: {StatusCode}. Response: {Response}",
                projectId, suiteId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get test cases for project {projectId} and suite {suiteId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var testCases = JsonSerializer.Deserialize<List<TestCollabTestCase>>(content);

        _logger.LogDebug("Found {Count} test cases", testCases!.Count);

        return testCases;
    }

    public async Task<List<TestCollabSharedStep>> GetSharedSteps(int projectId)
    {
        _logger.LogInformation("Getting shared steps for project {ProjectId}", projectId);

        var response = await _httpClient.GetAsync($"reusablesteps?project={projectId}&token={_token}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get shared steps for project {ProjectId}. Status code: {StatusCode}. Response: {Response}",
                projectId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get shared steps for project {projectId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var sharedSteps = JsonSerializer.Deserialize<List<TestCollabSharedStep>>(content);

        _logger.LogDebug("Found {Count} shared steps", sharedSteps!.Count);

        return sharedSteps;
    }

    public async Task<List<TestCollabCustomField>> GetCustomFields(int companyId)
    {
        _logger.LogInformation("Getting custom fields for company {CompanyId}", companyId);

        var response = await _httpClient.GetAsync($"customfields?company={companyId}&token={_token}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get custom fields for company {CompanyId}. Status code: {StatusCode}. Response: {Response}",
                companyId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to custom fields for company {companyId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var customFields = JsonSerializer.Deserialize<List<TestCollabCustomField>>(content);

        _logger.LogDebug("Found {Count} custom fields", customFields!.Count);

        return customFields;
    }

    public async Task<byte[]> DownloadAttachment(string link)
    {
        _logger.LogInformation("Downloading attachment {Link}", link);

        var httpClient = new HttpClient();
        return
            await httpClient.GetByteArrayAsync(link);
    }
}
