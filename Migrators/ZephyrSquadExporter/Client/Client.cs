using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZephyrSquadExporter.Models;

namespace ZephyrSquadExporter.Client;

public class Client : IClient
{
    private readonly ILogger<Client> _logger;
    private readonly TokenManager _tokenManager;
    private readonly string _baseUrl;
    private readonly string _projectId;
    private readonly string _accessKey;

    private const string Section = "/connect";

    public Client(ILogger<Client> logger, TokenManager tokenManager, IConfiguration configuration)
    {
        _logger = logger;
        _tokenManager = tokenManager;

        var section = configuration.GetSection("zephyr");
        var url = section["url"];
        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException("Url is not specified");
        }

        var projectId = section["projectId"];
        if (string.IsNullOrEmpty(projectId))
        {
            throw new ArgumentException("Project ID is not specified");
        }

        var accessKey = section["accessKey"];
        if (string.IsNullOrEmpty(accessKey))
        {
            throw new ArgumentException("Access key is not specified");
        }

        _baseUrl = url.Trim('/');
        _projectId = projectId;
        _accessKey = accessKey;
    }

    public async Task<List<ZephyrCycle>> GetCycles()
    {
        _logger.LogInformation("Getting cycles");

        var url = $"/public/rest/api/1.0/cycles/search?projectId={_projectId}&versionId=-1";
        var token = _tokenManager.GetToken("GET", url);

        var client = GetClient(token);

        var response = await client.GetAsync(Section + url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get cycles. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get cycles. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var cycles = JsonSerializer.Deserialize<List<ZephyrCycle>>(content);

        _logger.LogDebug("Found {Count} cycles: {Cycles}", cycles?.Count, cycles);

        return cycles;
    }

    public async Task<List<ZephyrFolder>> GetFolders(string cycleId)
    {
        _logger.LogInformation("Getting folders for cycle {CycleId}", cycleId);

        var url = $"/public/rest/api/1.0/folders?cycleId={cycleId}&projectId={_projectId}&versionId=-1";
        var token = _tokenManager.GetToken("GET", url);

        var client = GetClient(token);

        var response = await client.GetAsync(Section + url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to folders for cycle {CycleId}. Status code: {StatusCode}. Response: {Response}",
                cycleId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to folders for cycle {cycleId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var folders = JsonSerializer.Deserialize<List<ZephyrFolder>>(content);

        _logger.LogDebug("Found {Count} folders: {Folders}", folders?.Count, folders);

        return folders;
    }

    public async Task<List<ZephyrExecution>> GetTestCasesFromCycle(string cycleId)
    {
        _logger.LogInformation("Getting executions from cycle {CycleId}", cycleId);

        var url =
            $"/public/rest/api/2.0/executions/search/cycle/{cycleId}?offset=0&projectId={_projectId}&size=50&versionId=-1";

        var token = _tokenManager.GetToken("GET", url);

        var client = GetClient(token);

        var response = await client.GetAsync(Section + url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to executions from cycle {CycleId}. Status code: {StatusCode}. Response: {Response}",
                cycleId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to executions from cycle {cycleId}. Status code: {response.StatusCode}");
        }

        var listExecutions = new List<ZephyrExecution>();

        var content = await response.Content.ReadAsStringAsync();
        var executions = JsonSerializer.Deserialize<ZephyrExecutions>(content);

        if (executions?.SearchResult?.Executions != null)
        {
            listExecutions.AddRange(executions.SearchResult.Executions);
        }

        while (listExecutions.Count < executions?.SearchResult?.TotalCount)
        {
            url =
                $"/public/rest/api/2.0/executions/search/cycle/{cycleId}?offset={executions?.SearchResult?.CurrentOffset}&projectId={_projectId}&size=50&versionId=-1";
            token = _tokenManager.GetToken("GET", url);
            client = GetClient(token);

            response = await client.GetAsync(Section + url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to executions from cycle {CycleId}. Status code: {StatusCode}. Response: {Response}",
                    cycleId, response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception(
                    $"Failed to executions from cycle {cycleId}. Status code: {response.StatusCode}");
            }

            content = await response.Content.ReadAsStringAsync();
            executions = JsonSerializer.Deserialize<ZephyrExecutions>(content);
            listExecutions.AddRange(executions?.SearchResult.Executions);
        }

        _logger.LogDebug("Found {Count} executions: {Executions}", listExecutions?.Count, listExecutions);

        return listExecutions;
    }

    public async Task<List<ZephyrExecution>> GetTestCasesFromFolder(string cycleId, string folderId)
    {
        _logger.LogInformation("Getting executions from folder {FolderId}", folderId);

        var url =
            $"/public/rest/api/2.0/executions/search/folder/{folderId}?cycleId={cycleId}&offset=0&projectId={_projectId}&size=50&versionId=-1";

        var token = _tokenManager.GetToken("GET", url);

        var client = GetClient(token);

        var response = await client.GetAsync(Section + url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to executions from folder {FolderId}. Status code: {StatusCode}. Response: {Response}",
                folderId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to executions from folder {folderId}. Status code: {response.StatusCode}");
        }

        var listExecutions = new List<ZephyrExecution>();

        var content = await response.Content.ReadAsStringAsync();
        var executions = JsonSerializer.Deserialize<ZephyrExecutions>(content);

        if (executions?.SearchResult?.Executions != null)
        {
            listExecutions.AddRange(executions.SearchResult.Executions);
        }

        while (listExecutions.Count < executions?.SearchResult?.TotalCount)
        {
            url =
                $"/public/rest/api/2.0/executions/search/folder/{folderId}?cycleId={cycleId}&offset={executions?.SearchResult?.CurrentOffset}&projectId={_projectId}&size=50&versionId=-1";
            token = _tokenManager.GetToken("GET", url);
            client = GetClient(token);

            response = await client.GetAsync(Section + url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to executions from folder {FolderId}. Status code: {StatusCode}. Response: {Response}",
                    folderId, response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception(
                    $"Failed to executions from folder {folderId}. Status code: {response.StatusCode}");
            }

            content = await response.Content.ReadAsStringAsync();
            executions = JsonSerializer.Deserialize<ZephyrExecutions>(content);
            listExecutions.AddRange(executions?.SearchResult.Executions);
        }

        _logger.LogDebug("Found {Count} executions: {Executions}", listExecutions?.Count, listExecutions);

        return listExecutions;
    }

    public async Task<List<ZephyrStep>> GetSteps(string issueId)
    {
        _logger.LogInformation("Getting steps for issue {IssueId}", issueId);

        var url = $"/public/rest/api/2.0/teststep/{issueId}?projectId={_projectId}";
        var token = _tokenManager.GetToken("GET", url);

        var client = GetClient(token);

        var response = await client.GetAsync(Section + url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to steps for issue {IssueId}. Status code: {StatusCode}. Response: {Response}",
                issueId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to steps for issue {issueId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var steps = JsonSerializer.Deserialize<ZephyrSteps>(content);

        _logger.LogDebug("Found {Count} folders: {Folders}", steps?.Steps.Count, steps?.Steps);

        return steps?.Steps;
    }

    private HttpClient GetClient(string token)
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(_baseUrl);
        client.DefaultRequestHeaders.Add("Authorization", "JWT " + token);
        client.DefaultRequestHeaders.Add("zapiAccessKey", _accessKey);
        client.DefaultRequestHeaders.Add("User-Agent", "ZAPI");

        return client;
    }
}
