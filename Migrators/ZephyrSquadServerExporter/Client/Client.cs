using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ZephyrSquadServerExporter.Models;

namespace ZephyrSquadServerExporter.Client;

public class Client : IClient
{
    private readonly ILogger<Client> _logger;
    private readonly string _baseUrl;
    private readonly string _projectKey;
    private readonly string _token;
    private readonly HttpClient _client;

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
        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentException("Token is not specified");
        }
        _baseUrl = url.Trim('/');
        _projectKey = projectKey;
        _token = token;

        _client = new HttpClient();
        _client.BaseAddress = new Uri(_baseUrl);
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _token);
    }

    public async Task<JiraProject> GetProject()
    {
        _logger.LogInformation("Getting project by key {ProjectKey}", _projectKey);

        var response = await _client.GetAsync($"/rest/api/2/project/{_projectKey}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get project by key {ProjectKey}. Status code: {StatusCode}. Response: {Response}",
                _projectKey, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get project by key {_projectKey}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var project = JsonSerializer.Deserialize<JiraProject>(content);

        _logger.LogDebug("Found project {@ProjectId}", project);

        return project;
    }

    public async Task<string> GetZephyrIssueTypeIdByProjectId(string projectId)
    {
        _logger.LogInformation("Getting zephyr issue type id by project id {ProjectId}", projectId);

        var response = await _client.GetAsync($"/rest/api/2/issue/createmeta/{projectId}/issuetypes");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get zephyr issue type by project id {ProjectId}. Status code: {StatusCode}. Response: {Response}",
                projectId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get zephyr issue type id by project id {projectId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var issueTypes = JsonSerializer.Deserialize<JiraIssueTypes>(content);
        var zephyrIssueType = issueTypes.Types.First(i => i.Name.Equals("Test"));

        if (zephyrIssueType == null)
        {
            throw new Exception($"Failed to found zephyr issue type. All issue types {issueTypes}");
        }

        _logger.LogDebug("Found zephyr issue type id {Id}", zephyrIssueType.Id);

        return zephyrIssueType.Id;
    }

    public async Task<JiraIssueCustomAttributes> GetCustomAttributesByProjectIdAndZephyrIssueTypeId(string projectId, string zephyrIssueTypeId)
    {
        _logger.LogInformation("Getting custom attributes by project id {ProjectId} and zephyr issue type id {ZephyrIssueTypeId}", projectId, zephyrIssueTypeId);

        var response = await _client.GetAsync($"/rest/api/2/issue/createmeta/{projectId}/issuetypes/{zephyrIssueTypeId}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get custom attributes by project id {ProjectId} and zephyr issue type id {ZephyrIssueTypeId}. Status code: {StatusCode}. Response: {Response}",
                projectId, zephyrIssueTypeId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get custom attributes by project id {projectId} and zephyr issue type id {zephyrIssueTypeId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var jiraIssueCustomAttributes = JsonSerializer.Deserialize<JiraIssueCustomAttributes>(content);

        _logger.LogDebug("Found custom attributes {@CustomAttributes}", jiraIssueCustomAttributes);

        return jiraIssueCustomAttributes;
    }

    public async Task<List<ZephyrCycle>> GetCyclesByProjectIdAndVersionId(string projectId, string versionId)
    {
        _logger.LogInformation("Getting cycles by project id {ProjectId} and version id {VersionId}", projectId, versionId);

        var response = await _client.GetAsync($"/rest/zapi/latest/cycle?projectId={projectId}&versionId={versionId}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get cycles by project id {ProjectId} and version id {VersionId}. Status code: {StatusCode}. Response: {Response}",
                projectId, versionId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get cycles by project id {projectId} and version id {versionId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var cyclesDictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

        var cycles = new List<ZephyrCycle>();

        foreach (var cycleInfo in cyclesDictionary)
        {
            try
            {
                var cycle = JsonSerializer.Deserialize<ZephyrCycle>(cycleInfo.Value.ToString());
                cycle.Id = cycleInfo.Key.ToString();

                cycles.Add(cycle);
            }
            catch (Exception)
            {
                continue;
            }
        }

        _logger.LogDebug("Found {Count} cycles: {@Cycles}", cyclesDictionary?.Values.Count, cyclesDictionary);

        return cycles;
    }

    public async Task<List<ZephyrFolder>> GetFoldersByProjectIdAndVersionIdAndCycleId(string projectId, string versionId, string cycleId)
    {
        _logger.LogInformation("Getting folders for cycle {CycleId} by project id {ProjectId} and version id {VersionId}", cycleId, projectId, versionId);

        var response = await _client.GetAsync($"/rest/zapi/latest/cycle/{cycleId}/folders?projectId={projectId}&versionId={versionId}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to folders for cycle {CycleId} by project id {Id} and version id {VersionId}. Status code: {StatusCode}. Response: {Response}",
                cycleId, projectId, versionId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to folders for cycle {cycleId} by project id {projectId} and version id {versionId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var folders = JsonSerializer.Deserialize<List<ZephyrFolder>>(content);

        _logger.LogDebug("Found {Count} folders: {Folders}", folders?.Count, folders);

        return folders;
    }

    public async Task<List<ZephyrExecution>> GetTestCasesFromCycle(string projectId, string versionId, string cycleId)
    {
        _logger.LogInformation("Getting executions from cycle {CycleId} by project id {ProjectId} and version id {VersionId}", cycleId, projectId, versionId);

        var response = await _client
            .GetAsync($"rest/zapi/latest/execution?projectId={projectId}&versionId={versionId}&cycleId={cycleId}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to executions from cycle {CycleId} by project id {ProjectId} and version id {VersionId}. Status code: {StatusCode}. Response: {Response}",
                cycleId, projectId, versionId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to executions from cycle {cycleId} by project id {projectId} and version id {versionId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var executions = JsonSerializer.Deserialize<ZephyrExecutions>(content);

        _logger.LogDebug("Found {Count} executions: {Executions}", executions?.Executions.Count, executions);

        return executions.Executions;
    }

    public async Task<List<ZephyrExecution>> GetTestCasesFromFolder(string projectId, string versionId, string cycleId, string folderId)
    {
        _logger.LogInformation("Getting executions from folder {FolderId} by project id {ProjectId}, cycle id {CycleId} and version id {VersionId}", folderId, projectId, cycleId, versionId);

        var response = await _client
            .GetAsync($"rest/zapi/latest/execution?projectId={projectId}&versionId={versionId}&cycleId={cycleId}&folderId={folderId}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to executions from folder {FolderId} by project id {ProjectId}, cycle id {CycleId} and version id {VersionId}. Status code: {StatusCode}. Response: {Response}",
                folderId, projectId, cycleId, versionId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to executions from folder {folderId} by project id {projectId}, cycle id {cycleId} and version id {versionId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var executions = JsonSerializer.Deserialize<ZephyrExecutions>(content);

        _logger.LogDebug("Found {Count} executions: {Executions}", executions?.Executions.Count, executions);

        return executions.Executions;
    }

    public async Task<List<ZephyrStep>> GetSteps(string issueId)
    {
        _logger.LogInformation("Getting steps for issue {IssueId}", issueId);

        var response = await _client.GetAsync($"/rest/zapi/latest/teststep/{issueId}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to steps for issue {IssueId}. Status code: {StatusCode}. Response: {Response}",
                issueId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to steps for issue {issueId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var steps = JsonSerializer.Deserialize<ZephyrSteps>(content);

        _logger.LogDebug("Found {Count} steps: {Steps}", steps?.Steps.Count, steps?.Steps);

        return steps?.Steps;
    }

    public async Task<JiraIssue> GetIssueById(string issueId)
    {
        _logger.LogInformation("Getting issue by id {IssueId}", issueId);

        var response = await _client.GetAsync($"/rest/api/2/issue/{issueId}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to attachments for issue {IssueId}. Status code: {StatusCode}. Response: {Response}",
                issueId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get issue by id {issueId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var issue = JsonSerializer.Deserialize<JiraIssue>(content);

        _logger.LogDebug("Got issue: {@Issue}", issue);

        return issue;
    }

    public async Task<byte[]> GetAttachmentForIssueById(string fileId, string fileName)
    {
        _logger.LogInformation("Getting attachment for issue by id {FileId}", fileId);

        var response = await _client
            .GetByteArrayAsync($"/secure/attachment/{fileId}/{fileName}");

        return response;
    }

    public async Task<byte[]> GetAttachmentForStepById(string fileId)
    {
        _logger.LogInformation("Getting attachment for step by id {FileId}", fileId);

        var response = await _client
            .GetByteArrayAsync($"/rest/zapi/latest/attachment/{fileId}/file");

        return response;
    }
}
