using System.Text.Json;
using PractiTestExporter.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PractiTestExporter.Client;

public class Client : IClient
{
    private readonly ILogger<Client> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _projectId;
    private const int requestDelay = 2; 

    public Client(ILogger<Client> logger, IConfiguration configuration)
    {
        _logger = logger;

        var section = configuration.GetSection("practiTest");
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

        var projectId = section["projectId"];
        if (string.IsNullOrEmpty(projectId))
        {
            throw new ArgumentException("Project name is not specified");
        }

        _projectId = projectId;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(url);
        _httpClient.DefaultRequestHeaders.Add("PTToken", token);
    }

    public async Task<PractiTestProject> GetProject()
    {
        await Task.Delay(TimeSpan.FromSeconds(requestDelay));

        _logger.LogInformation("Getting project with id {Id}", _projectId);

        var response = await _httpClient.GetAsync($"api/v2/projects/{_projectId}.json");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get project. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get project. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var project = JsonSerializer.Deserialize<PractiTestProject>(content);

        if (project != null) return project;

        _logger.LogError("Project not found");

        throw new Exception("Project not found");
    }

    public async Task<List<PractiTestTestCase>> GetTestCases()
    {
        await Task.Delay(TimeSpan.FromSeconds(requestDelay));

        _logger.LogInformation("Getting test cases from project {Id}", _projectId);

        var response =
            await _httpClient.GetAsync($"api/v2/projects/{_projectId}/tests.json");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get test case ids from project {Id}. Status code: {StatusCode}. Response: {Response}",
                _projectId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get test case ids from project {_projectId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var testCases = JsonSerializer.Deserialize<PractiTestTestCases>(content);

        return testCases is { Data.Count: 0 }
            ? new List<PractiTestTestCase>()
            : testCases.Data;
    }

    public async Task<PractiTestTestCase> GetTestCaseById(string id)
    {
        await Task.Delay(TimeSpan.FromSeconds(requestDelay));

        _logger.LogInformation("Getting test case by id {Id}", id);

        var response =
            await _httpClient.GetAsync($"api/v2/projects/{_projectId}/tests/{id}.json");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get test case by id {Id}. Status code: {StatusCode}. Response: {Response}",
                id, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get test case by id {id}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var testCase = JsonSerializer.Deserialize<SinglePractiTestTestCase>(content);

        return testCase.Data;
    }

    public async Task<List<PractiTestStep>> GetStepsByTestCaseId(string testCaseId)
    {
        await Task.Delay(TimeSpan.FromSeconds(requestDelay));

        _logger.LogInformation("Getting steps for test case with id {Id}", testCaseId);

        var response = await _httpClient.GetAsync($"api/v2/projects/{_projectId}/steps.json?test-ids={testCaseId}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get steps for test case with id {TestCaseId}. Status code: {StatusCode}. Response: {Response}",
                testCaseId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get steps for test case with id {testCaseId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var steps = JsonSerializer.Deserialize<PractiTestSteps>(content);

        return steps is { Data.Count: 0 }
            ? new List<PractiTestStep>()
            : steps.Data;
    }

    public async Task<List<PractiTestAttachment>> GetAttachmentsByEntityId(string entityType, string entityId)
    {
        await Task.Delay(TimeSpan.FromSeconds(requestDelay));

        _logger.LogInformation("Getting attachments for {EntityType} with id {EntityId}", entityType, entityId);

        var response = await _httpClient.GetAsync($"api/v2/projects/{_projectId}/attachments.json?entity={entityType}&entity-id={entityId}");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get attachments for {EntityType} with id {EntityId}. Status code: {StatusCode}. Response: {Response}",
                entityType, entityId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get attachments for {entityType} with id {entityId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var attachments = JsonSerializer.Deserialize<PractiTestAttachments>(content);

        return attachments is { Data.Count: 0 }
            ? new List<PractiTestAttachment>()
            : attachments.Data;
    }

    public async Task<byte[]> DownloadAttachmentById(string id)
    {
        await Task.Delay(TimeSpan.FromSeconds(requestDelay));

        _logger.LogInformation("Downloading attachment by id {Id}", id);

        return await _httpClient.GetByteArrayAsync($"api/v2/projects/{_projectId}/attachments/{id}");
    }

    public async Task<List<PractiTestCustomField>> GetCustomFields()
    {
        await Task.Delay(TimeSpan.FromSeconds(requestDelay));

        _logger.LogInformation("Getting custom fields by project id {ProjectId}", _projectId);

        var response = await _httpClient.GetAsync($"api/v2/projects/{_projectId}/custom_fields.json");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get custom fields by project id {ProjectId}. Status code: {StatusCode}. Response: {Response}",
                _projectId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get custom fields by project id {_projectId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var customFields = JsonSerializer.Deserialize<PractiTestCustomFields>(content);

        return customFields is { Data.Count: 0 }
            ? new List<PractiTestCustomField>()
            : customFields.Data;
    }

    public async Task<ListPractiTestCustomField> GetListCustomFieldById(string id)
    {
        await Task.Delay(TimeSpan.FromSeconds(requestDelay));

        _logger.LogInformation("Getting custom field by id {Id}", id);

        var response = await _httpClient.GetAsync($"api/v2/projects/{_projectId}/custom_fields//{id}.json");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get custom field by id {Id}. Status code: {StatusCode}. Response: {Response}",
                id, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get custom field by id {id}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var customField = JsonSerializer.Deserialize<SinglePractiTestCustomField>(content);

        return customField.Data;
    }
}
