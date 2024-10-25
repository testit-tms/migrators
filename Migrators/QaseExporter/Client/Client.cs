using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QaseExporter.Models;

namespace QaseExporter.Client;

public class Client : IClient
{
    private readonly ILogger<Client> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _projectKey;

    public Client(ILogger<Client> logger, IConfiguration configuration)
    {
        _logger = logger;

        var section = configuration.GetSection("qase");
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
        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentException("Token is not specified");
        }

        _projectKey = projectKey;

        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(url);
        _httpClient.DefaultRequestHeaders.Add("Token", token);
    }

    public async Task<QaseProject> GetProject()
    {
        _logger.LogInformation("Getting project by key {ProjectKey}", _projectKey);

        var response = await _httpClient.GetAsync($"/v1/project/{_projectKey}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get project by key {ProjectKey}. Status code: {StatusCode}. Response: {Response}",
                _projectKey, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get project by key {_projectKey}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var projectData = JsonSerializer.Deserialize<QaseProjectData>(content);

        _logger.LogDebug("Found project {@Project}", projectData.Project);

        return projectData.Project;
    }

    public async Task<List<QaseSuite>> GetSuites()
    {
        _logger.LogInformation("Getting suites by project key {Key}", _projectKey);

        var allSuites = new List<QaseSuite>();
        var startAt = 0;
        var maxResults = 100;
        var countOfSuites = 0;
        var total = 0;

        do
        {
            var response = await _httpClient.GetAsync($"/v1/suite/{_projectKey}?limit={maxResults}&offset={startAt}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get suites by project key {Key}. Status code: {StatusCode}. Response: {Response}",
                    _projectKey, response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception($"Failed to get suites by project key {_projectKey}. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var suitesData = JsonSerializer.Deserialize<QaseSuitesData>(content);

            if (suitesData.SuitesData.Count > 0)
            {
                _logger.LogDebug("Got suites {@Suites}", suitesData.SuitesData.Suites);

                allSuites.AddRange(suitesData.SuitesData.Suites);
                startAt += maxResults;
                total = suitesData.SuitesData.Filtered;
                countOfSuites += suitesData.SuitesData.Count;
            }
            else
            {
                startAt = -1;
            }

            _logger.LogInformation("Got {Count} out of {Total} suites", countOfSuites, total);
        } while (countOfSuites < total && startAt >= 0);

        return allSuites;
    }

    public async Task<List<QaseTestCase>> GetTestCasesBySuiteId(int suiteId)
    {
        _logger.LogInformation("Getting test cases by suite id {Id}", suiteId);

        var allTestCases = new List<QaseTestCase>();
        var startAt = 0;
        var maxResults = 100;
        var countOfTests = 0;
        var total = 0;

        do
        {
            var response = await _httpClient.GetAsync($"/v1/case/{_projectKey}?limit={maxResults}&offset={startAt}&suite_id={suiteId}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get test cases by suite id {Id}. Status code: {StatusCode}. Response: {Response}",
                    suiteId, response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception($"Failed to get test cases by suite id {suiteId}. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var qaseCasesData = JsonSerializer.Deserialize<QaseCasesData>(content);

            if (qaseCasesData.CasesData.Count > 0)
            {
                _logger.LogDebug("Got test cases {@Cases}", qaseCasesData.CasesData.Cases);

                allTestCases.AddRange(qaseCasesData.CasesData.Cases);
                startAt += maxResults;
                total = qaseCasesData.CasesData.Filtered;
                countOfTests += qaseCasesData.CasesData.Count;
            }
            else
            {
                startAt = -1;
            }

            _logger.LogInformation("Got {Count} out of {Total} test cases", countOfTests, total);
        } while (countOfTests < total && startAt >= 0);

        return allTestCases;
    }

    public async Task<List<QaseSharedStep>> GetSharedSteps()
    {
        _logger.LogInformation("Getting shared steps");

        var allSharedSteps = new List<QaseSharedStep>();
        var startAt = 0;
        var maxResults = 100;
        var countOfSharedSteps = 0;
        var total = 0;

        do
        {
            var response = await _httpClient.GetAsync($"/v1/shared_step/{_projectKey}?limit={maxResults}&offset={startAt}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get shared steps. Status code: {StatusCode}. Response: {Response}",
                    response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception($"Failed to get shared steps. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var sharedStepData = JsonSerializer.Deserialize<SharedStepData>(content);

            if (sharedStepData.SharedStepsData.Count > 0)
            {
                _logger.LogDebug("Got shared steps {@SharedSteps}", sharedStepData.SharedStepsData.SharedSteps);

                allSharedSteps.AddRange(sharedStepData.SharedStepsData.SharedSteps);
                startAt += maxResults;
                total = sharedStepData.SharedStepsData.Filtered;
                countOfSharedSteps += sharedStepData.SharedStepsData.Count;
            }
            else
            {
                startAt = -1;
            }

            _logger.LogInformation("Got {Count} out of {Total} shared steps", countOfSharedSteps, total);
        } while (countOfSharedSteps < total && startAt >= 0);

        return allSharedSteps;
    }

    public async Task<List<QaseCustomField>> GetCustomFields()
    {
        _logger.LogInformation("Getting custom fields");

        var allCustomFields = new List<QaseCustomField>();
        var startAt = 0;
        var maxResults = 100;
        var countOfFields = 0;
        var total = 0;

        do
        {
            var response = await _httpClient.GetAsync($"/v1/custom_field?limit={maxResults}&offset={startAt}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get custom fields. Status code: {StatusCode}. Response: {Response}",
                    response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception($"Failed to get custom fields. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var fieldsData = JsonSerializer.Deserialize<QaseFieldsData>(content);

            if (fieldsData.FieldsData.Count > 0)
            {
                _logger.LogDebug("Got custom fields {@Fields}", fieldsData.FieldsData.Fields);

                allCustomFields.AddRange(fieldsData.FieldsData.Fields);
                startAt += maxResults;
                total = fieldsData.FieldsData.Filtered;
                countOfFields += fieldsData.FieldsData.Count;
            }
            else
            {
                startAt = -1;
            }

            _logger.LogInformation("Got {Count} out of {Total} custom fields", countOfFields, total);
        } while (countOfFields < total && startAt >= 0);

        return allCustomFields;
    }

    public async Task<List<QaseSystemField>> GetSystemFields()
    {
        _logger.LogInformation("Getting system fields");

        var response = await _httpClient.GetAsync($"/v1/system_field");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get system fields. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get system fields. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var systemFieldsData = JsonSerializer.Deserialize<QaseSysFieldsData>(content);

        _logger.LogDebug("Got system fields: {@Fields}", systemFieldsData.Fields);

        return systemFieldsData.Fields;
    }

    public async Task<byte[]> DownloadAttachment(string url)
    {
        return await _httpClient.GetByteArrayAsync(url);
    }

    public string GetProjectKey()
    {
        return _projectKey;
    }
}
