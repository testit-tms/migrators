using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpiraTestExporter.Models;

namespace SpiraTestExporter.Client;

public class Client : IClient
{
    private readonly ILogger<Client> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _projectName;

    public Client(ILogger<Client> logger, IConfiguration configuration)
    {
        _logger = logger;

        var section = configuration.GetSection("spiraTest");
        var url = section["url"];
        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException("Url is not specified");
        }

        var username = section["username"];
        if (string.IsNullOrEmpty(username))
        {
            throw new ArgumentException("Username is not specified");
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

        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(url);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("username", username);
        _httpClient.DefaultRequestHeaders.Add("api-key", token);

    }

    public async Task<SpiraProject> GetProject()
    {
        _logger.LogInformation("Getting project {ProjectName}", _projectName);

        var response = await _httpClient.GetAsync("Services/v7_0/RestService.svc/projects");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get project. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get project. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var projects = JsonSerializer.Deserialize<List<SpiraProject>>(content);
        var project = projects!.FirstOrDefault(p =>
            string.Equals(p.Name, _projectName, StringComparison.InvariantCultureIgnoreCase));

        if (project == null)
        {
            throw new Exception($"Project {_projectName} not found");
        }

        return project;
    }

    public async Task<List<SpiraFolder>> GetFolders(int projectId)
    {
        _logger.LogInformation("Getting folders for project {ProjectId}", projectId);

        var response = await _httpClient.GetAsync($"Services/v7_0/RestService.svc/projects/{projectId}/test-folders");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get folders. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get folders. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var folders = JsonSerializer.Deserialize<List<SpiraFolder>>(content);

        _logger.LogDebug("Got {Count} folders: {@Folders}", folders?.Count, folders);

        return folders!;
    }

    public async Task<List<SpiraTest>> GetTestFromFolder(int projectId, int folderId)
    {
        _logger.LogInformation("Getting tests from folder {FolderId} for project {ProjectId}", folderId, projectId);

        var response = await _httpClient.GetAsync($"Services/v7_0/RestService.svc/projects/{projectId}/test-folders/{folderId}/test-cases?starting_row=1&number_of_rows=2000");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get tests. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get tests. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var tests = JsonSerializer.Deserialize<List<SpiraTest>>(content);

        _logger.LogDebug("Got {Count} tests: {@Tests}", tests?.Count, tests);

        return tests!;
    }

    public async Task<SpiraTest> GetTestById(int projectId, int id)
    {
        _logger.LogInformation("Getting tests {Id} for project {ProjectId}", id, projectId);

        var response = await _httpClient.GetAsync($"Services/v7_0/RestService.svc/projects/{projectId}/test-cases/{id}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get test. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get test. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var test = JsonSerializer.Deserialize<SpiraTest>(content);

        return test!;
    }

    public async Task<List<SpiraPriority>> GetPriorities(int projectTemplateId)
    {
        _logger.LogInformation("Getting priorities for project {ProjectId}", projectTemplateId);

        var response = await _httpClient.GetAsync($"Services/v7_0/RestService.svc/project-templates/{projectTemplateId}/test-cases/priorities");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get priorities. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get priorities. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var priorities = JsonSerializer.Deserialize<List<SpiraPriority>>(content);

        _logger.LogDebug("Got {Count} priorities: {@Priorities}", priorities?.Count, priorities);

        return priorities!;
    }

    public async Task<List<SpiraStatus>> GetStatuses(int projectTemplateId)
    {
        _logger.LogInformation("Getting statuses for project {ProjectId}", projectTemplateId);

        var response = await _httpClient.GetAsync($"Services/v7_0/RestService.svc/project-templates/{projectTemplateId}/test-cases/statuses");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get statuses. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get statuses. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var statuses = JsonSerializer.Deserialize<List<SpiraStatus>>(content);

        _logger.LogDebug("Got {Count} statuses: {@Statuses}", statuses?.Count, statuses);

        return statuses!;
    }

    public async Task<List<SpiraStep>> GetTestSteps(int projectId, int testCaseId)
    {
        _logger.LogInformation("Getting test steps for project {ProjectId} and test case {TestCaseId}", projectId, testCaseId);

        var response = await _httpClient.GetAsync($"Services/v7_0/RestService.svc/projects/{projectId}/test-cases/{testCaseId}/test-steps");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get test steps. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get test steps. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var testSteps = JsonSerializer.Deserialize<List<SpiraStep>>(content);

        _logger.LogDebug("Got {Count} test steps: {@TestSteps}", testSteps?.Count, testSteps);

        return testSteps!;
    }

    public async Task<List<SpiraTestCaseParameter>> GetSpiraParameters(int projectId, int testCaseId)
    {
        _logger.LogInformation("Getting parameters for project {ProjectId} and test case {TestCaseId}", projectId, testCaseId);

        var response = await _httpClient.GetAsync($"Services/v7_0/RestService.svc/projects/{projectId}/test-cases/{testCaseId}/parameters");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get parameters. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get parameters. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var parameters = JsonSerializer.Deserialize<List<SpiraTestCaseParameter>>(content);

        _logger.LogDebug("Got {Count} parameters: {@TestCaseParameter}", parameters?.Count, parameters);

        return parameters!;
    }

    public async Task<List<SpiraStepParameter>> GetStepParameters(int projectId, int testCaseId, int stepId)
    {
        _logger.LogInformation("Getting parameters for project {ProjectId} and test case {TestCaseId} and step {StepId}", projectId, testCaseId, stepId);

        var response = await _httpClient.GetAsync($"Services/v7_0/RestService.svc/projects/{projectId}/test-cases/{testCaseId}/test-steps/{stepId}/parameters");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get parameters. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get parameters. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var parameters = JsonSerializer.Deserialize<List<SpiraStepParameter>>(content);

        _logger.LogDebug("Got {Count} parameters: {@StepParameters}", parameters?.Count, parameters);

        return parameters!;
    }

    public async Task<List<SpiraAttachment>> GetAttachments(int projectId, int artifactTypeId, int artifactId)
    {
        _logger.LogInformation("Getting attachments for project {ProjectId} and item {ArtifactId}", projectId, artifactId);

        var response = await _httpClient.GetAsync($"Services/v7_0/RestService.svc/projects/{projectId}/artifact-types/{artifactTypeId}/artifacts/{artifactId}/documents");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get attachments. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get attachments. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var attachments = JsonSerializer.Deserialize<List<SpiraAttachment>>(content);

        _logger.LogDebug("Got {Count} attachments: {@Attachments}", attachments?.Count, attachments);

        return attachments!;
    }

    public async Task<byte[]> DownloadAttachment(int projectId, int attachmentId)
    {
        _logger.LogInformation("Downloading attachment {AttachmentId} for project {ProjectId}", attachmentId, projectId);

        return await _httpClient.GetByteArrayAsync($"Services/v7_0/RestService.svc/projects/{projectId}/documents/{attachmentId}/open");
    }
}
