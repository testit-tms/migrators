using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using XRayExporter.Models;

namespace XRayExporter.Client;

public class Client : IClient
{
    private readonly ILogger<Client> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _projectKey;

    public Client(ILogger<Client> logger, IConfiguration configuration)
    {
        _logger = logger;

        var section = configuration.GetSection("xray");
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

        var projectKey = section["projectKey"];
        if (string.IsNullOrEmpty(projectKey))
        {
            throw new ArgumentException("Project key is not specified");
        }

        _projectKey = projectKey;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(CorrectBaseAddress(url));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<JiraProject> GetProject()
    {
        _logger.LogInformation("Getting project {ProjectKey}", _projectKey);

        var response = await _httpClient.GetAsync("rest/api/2/project");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get project. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get project. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var projects = JsonSerializer.Deserialize<List<JiraProject>>(content);
        var project = projects!.FirstOrDefault(p =>
            string.Equals(p.Key, _projectKey, StringComparison.InvariantCultureIgnoreCase));

        if (project != null) return project;

        _logger.LogError("Project not found");

        throw new Exception("Project not found");
    }

    public async Task<List<XrayFolder>> GetFolders()
    {
        _logger.LogInformation("Getting folders for project {ProjectKey}", _projectKey);

        var response =
            await _httpClient.GetAsync($"rest/raven/1.0/api/testrepository/{_projectKey.ToUpper()}/folders");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get folders. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get folders. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var folders = JsonSerializer.Deserialize<XRayFolders>(content);

        return folders!.Folders;
    }

    public async Task<List<XRayTest>> GetTestFromFolder(int folderId)
    {
        _logger.LogInformation("Getting test from folder {FolderId}", folderId);

        var response =
            await _httpClient.GetAsync(
                $"rest/raven/1.0/api/testrepository/{_projectKey.ToUpper()}/folders/{folderId}/tests");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get test from folder {FolderId}. Status code: {StatusCode}. Response: {Response}",
                folderId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get test from folder {folderId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var tests = JsonSerializer.Deserialize<XRayTests>(content);

        return tests!.Tests;
    }

    public async Task<XRayTestFull> GetTest(string testKey)
    {
        _logger.LogInformation("Getting test {TestKey}", testKey);

        var response =
            await _httpClient.GetAsync(
                $"rest/raven/1.0/api/test?keys={testKey}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get test {TestKey}. Status code: {StatusCode}. Response: {Response}",
                testKey, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get test {testKey}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var tests = JsonSerializer.Deserialize<List<XRayTestFull>>(content);

        return tests!.First();
    }

    public async Task<JiraItem> GetItem(string link)
    {
        _logger.LogInformation("Getting item {Link}", link);

        var response =
            await _httpClient.GetAsync(link.Split(_httpClient.BaseAddress.ToString())[1]);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get item {Link}. Status code: {StatusCode}. Response: {Response}",
                link, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get item {link}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var item = JsonSerializer.Deserialize<JiraItem>(content);

        return item;
    }

    public async Task<byte[]> DownloadAttachment(string link)
    {
        _logger.LogInformation("Downloading attachment {Link}", link);

        return
            await _httpClient.GetByteArrayAsync(link.Split(_httpClient.BaseAddress.ToString())[1]);
    }

    private string CorrectBaseAddress(string url)
    {
        if (url.EndsWith('/'))
        {
            return url;
        }
        return url + '/';
    }
}
