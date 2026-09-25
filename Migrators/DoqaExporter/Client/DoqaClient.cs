using System.Net.Http.Headers;
using System.Net.Http.Json;
using DoqaExporter.Models;
using Microsoft.Extensions.Logging;

namespace DoqaExporter.Client;

public class DoqaClient : IDoqaClient
{
    private readonly HttpClient _httpClient;
    private readonly DoqaConfig _config;
    private readonly ILogger<DoqaClient> _logger;
    private string? _token;
    private int? _userId;
    private string? _fileBaseUrl;

    public DoqaClient(HttpClient httpClient, DoqaConfig config, ILogger<DoqaClient> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(config.Url.TrimEnd('/'));
    }

    public async Task<string> Login()
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", new
        {
            email = _config.Email,
            password = _config.Password
        });
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<DoqaLoginResponse>();
        _token = result!.Tokens.AccessToken;
        _userId = result.User.Id;

        SetAuthHeaders();

        _logger.LogInformation("Авторизация успешна: {FirstName} {LastName}",
            result.User.FirstName, result.User.LastName);

        return _token;
    }

    public async Task<string> GetFileBaseUrl()
    {
        if (_fileBaseUrl != null) return _fileBaseUrl;

        var response = await SendGetAsync("/api/config/get");
        var config = await response.Content.ReadFromJsonAsync<DoqaConfigResponse>();
        _fileBaseUrl = config?.FileSystemLink ?? string.Empty;
        return _fileBaseUrl;
    }

    public async Task<List<DoqaProject>> GetProjects()
    {
        var response = await SendGetAsync("/api/projects");
        var result = await response.Content.ReadFromJsonAsync<DoqaProjectsResponse>();
        return result?.Projects ?? new List<DoqaProject>();
    }

    public async Task<DoqaFolderTree> GetFolders(int spaceId)
    {
        var response = await SendGetAsync($"/api/folders/space/{spaceId}/case");
        var result = await response.Content.ReadFromJsonAsync<DoqaFolderTree>();
        return result ?? new DoqaFolderTree();
    }

    public async Task<List<int>> GetAllCaseIds(int spaceId)
    {
        var ids = new List<int>();
        await FetchFolderCases(spaceId, null, ids);
        _logger.LogInformation("Найдено {Count} кейсов в space {SpaceId}", ids.Count, spaceId);
        return ids;
    }

    public async Task<DoqaTestCase> GetCase(int caseId)
    {
        var response = await SendGetAsync($"/api/cases/{caseId}");
        var result = await response.Content.ReadFromJsonAsync<DoqaTestCase>();
        return result ?? throw new InvalidOperationException($"Failed to load case {caseId}");
    }

    public async Task<byte[]?> DownloadAttachment(string path)
    {
        // CDN требует запрос БЕЗ авторизации (с токеном возвращает 400)
        var baseUrl = await GetFileBaseUrl();
        var url = $"{baseUrl}{path}";

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsByteArrayAsync();

            _logger.LogWarning("Аттач {Path}: HTTP {Status}", path, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Аттач {Path}: {Message}", path, ex.Message);
        }

        return null;
    }

    // -- Checklists --------------------------------------------------------

    public async Task<DoqaFolderTree> GetChecklistFolders(int spaceId)
    {
        var response = await SendGetAsync($"/api/folders/space/{spaceId}/checklist");
        var result = await response.Content.ReadFromJsonAsync<DoqaFolderTree>();
        return result ?? new DoqaFolderTree();
    }

    public async Task<List<int>> GetAllChecklistIds(int spaceId)
    {
        var ids = new List<int>();
        await FetchFolderChecklists(spaceId, null, ids);
        _logger.LogInformation("Найдено {Count} чеклистов в space {SpaceId}", ids.Count, spaceId);
        return ids;
    }

    public async Task<DoqaChecklist> GetChecklist(int checklistId)
    {
        var response = await SendGetAsync($"/api/checklists/{checklistId}");
        var result = await response.Content.ReadFromJsonAsync<DoqaChecklist>();
        return result ?? throw new InvalidOperationException($"Failed to load checklist {checklistId}");
    }

    private async Task FetchFolderChecklists(int spaceId, int? folderId, List<int> ids)
    {
        var body = new Dictionary<string, object?>
        {
            ["spaceId"] = spaceId,
            ["createdAt"] = null,
            ["changedAt"] = null,
            ["excludeIds"] = Array.Empty<int>()
        };

        if (folderId.HasValue)
            body["folderId"] = folderId.Value;

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/checklists/list")
        {
            Content = JsonContent.Create(body)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<DoqaChecklistsListResponse>();
        var rootNode = result?.Data;
        if (rootNode == null) return;

        foreach (var child in rootNode.Children)
        {
            var data = child.Data;
            if (data == null) continue;

            if (data.IsFolder)
                await FetchFolderChecklists(spaceId, data.Id, ids);
            else
                ids.Add(data.Id);
        }
    }

    // -- Private helpers ---------------------------------------------------

    private void SetAuthHeaders()
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        if (_userId.HasValue)
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-user-id", _userId.Value.ToString());

        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-time-zone", "+00:00");
    }

    private async Task<HttpResponseMessage> SendGetAsync(string path)
    {
        var response = await _httpClient.GetAsync(path);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private async Task FetchFolderCases(int spaceId, int? folderId, List<int> ids)
    {
        var body = new Dictionary<string, object?>
        {
            ["spaceId"] = spaceId,
            ["createdAt"] = null,
            ["changedAt"] = null,
            ["excludeIds"] = Array.Empty<int>()
        };

        if (folderId.HasValue)
            body["folderId"] = folderId.Value;

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/cases/list")
        {
            Content = JsonContent.Create(body)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<DoqaCasesListResponse>();
        var rootNode = result?.Data;
        if (rootNode == null) return;

        foreach (var child in rootNode.Children)
        {
            var data = child.Data;
            if (data == null) continue;

            if (data.IsFolder)
            {
                await FetchFolderCases(spaceId, data.Id, ids);
            }
            else
            {
                ids.Add(data.Id);
            }
        }
    }
}
