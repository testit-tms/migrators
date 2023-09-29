using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZephyrScaleExporter.Models;

namespace ZephyrScaleExporter.Client;

public class Client : IClient
{
    private readonly ILogger<Client> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _projectName;

    public Client(ILogger<Client> logger, IConfiguration configuration)
    {
        _logger = logger;

        var section = configuration.GetSection("zephyr");
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
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(url);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<ZephyrProject> GetProject()
    {
        _logger.LogInformation("Getting project {projectName}", _projectName);

        var response = await _httpClient.GetAsync("projects");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get project. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get project. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var projects = JsonSerializer.Deserialize<ZephyrProjects>(content);
        var project = projects?.Projects.FirstOrDefault(p =>
            string.Equals(p.Key, _projectName, StringComparison.InvariantCultureIgnoreCase));

        if (project != null) return project;

        _logger.LogError("Project not found");

        throw new Exception("Project not found");
    }

    public async Task<List<ZephyrStatus>> GetStatuses()
    {
        _logger.LogInformation("Getting statuses");

        var response = await _httpClient.GetAsync($"statuses?projectKey={_projectName}&statusType=TEST_CASE");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get statuses. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get statuses. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var statuses = JsonSerializer.Deserialize<ZephyrStatuses>(content);

        _logger.LogDebug("Got statuses {@Statuses}", statuses);

        return statuses.Statuses;
    }

    public async Task<List<ZephyrPriority>> GetPriorities()
    {
        _logger.LogInformation("Getting priorities");

        var response = await _httpClient.GetAsync($"priorities?projectKey={_projectName}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get priorities. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get priorities. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var priorities = JsonSerializer.Deserialize<ZephyrPriorities>(content);

        _logger.LogDebug("Got priorities {@Priorities}", priorities);

        return priorities.Priorities;
    }
}
