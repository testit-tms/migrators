using System.Net.Http.Headers;
using System.Text.Json;
using AllureExporter.Models.Attachment;
using AllureExporter.Models.Comment;
using AllureExporter.Models.Config;
using AllureExporter.Models.Project;
using AllureExporter.Models.Relation;
using AllureExporter.Models.Step;
using AllureExporter.Models.TestCase;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AllureExporter.Client;

/// <summary>
/// Client for interacting with Allure API.
/// </summary>
internal class Client : IClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Client> _logger;
    private readonly bool _migrateAutotests;
    private readonly string _projectName;

    public Client(ILogger<Client> logger, IOptions<AppConfig> config, HttpClient httpClient)
    {
        _logger = logger;
        _projectName = config.Value.Allure.ProjectName;
        _migrateAutotests = config.Value.Allure.MigrateAutotests;
        _httpClient = httpClient;
        InitClient(config.Value);
    }

    private void InitClient(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config.Allure.Url);

        var url = config.Allure.Url;
        var apiToken = config.Allure.ApiToken;
        var bearerToken = config.Allure.BearerToken;

        _httpClient.BaseAddress = new Uri(CorrectBaseAddress(url));

        if (!string.IsNullOrEmpty(apiToken))
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Api-Token", apiToken);
        else if (!string.IsNullOrEmpty(bearerToken))
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", bearerToken);
        else
            throw new ArgumentException("Api-Token or Bearer-Token is not specified");
    }

    public async Task<BaseEntity> GetProjectId()
    {
        _logger.LogInformation("Getting project id with name {Name}", _projectName);

        var response = await _httpClient.GetAsync("api/rs/project");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get project id. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get project id. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var projects = JsonSerializer.Deserialize<BaseEntities>(content);
        var project = projects?.Content.FirstOrDefault(p =>
            string.Equals(p.Name, _projectName, StringComparison.InvariantCultureIgnoreCase));

        if (project != null) return project;

        _logger.LogError("Project not found");

        throw new Exception("Project not found");
    }


    public async Task<List<long>> GetTestCaseIdsFromMainSuite(long projectId)
    {
        var testCaseIds = new List<long>();
        var page = 0;
        long totalPages = -1;

        _logger.LogInformation("Getting test case ids from main suite");

        do
        {
            var response = await _httpClient.GetAsync(
                $"api/rs/testcasetree/leaf?projectId={projectId}&treeId=2&page={page}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get test case ids from main suite. Status code: {StatusCode}. Response: {Response}",
                    response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception($"Failed to get test case ids from main suite. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var testCases = JsonSerializer.Deserialize<AllureTestCases>(content)!;
            totalPages = testCases.TotalPages;

            testCaseIds.AddRange(testCases.Content
                .Where(t => _migrateAutotests || t.Automated == false)
                .Select(t => t.Id)
                .ToList());

            page++;

            _logger.LogInformation("Got test case ids from main suite from {Page} " +
                                   "page out of {TotalPages} pages", page, testCases.TotalPages);
        } while (page < totalPages);

        return testCaseIds;
    }

    public async Task<List<long>> GetTestCaseIdsFromSuite(long projectId, long suiteId)
    {
        var testCaseIds = new List<long>();
        var page = 0;
        var totalPages = -1L;

        _logger.LogInformation("Getting test case ids from suite {SuiteId}", suiteId);

        do
        {
            var response =
                await _httpClient.GetAsync(
                    $"api/rs/testcasetree/leaf?projectId={projectId}&treeId=2&path={suiteId}&page={page}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get test case ids from suite {SuiteId}. Status code: {StatusCode}. Response: {Response}",
                    suiteId, response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception(
                    $"Failed to get test case ids from suite {suiteId}. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var testCases = JsonSerializer.Deserialize<AllureTestCases>(content)!;
            totalPages = testCases.TotalPages;

            testCaseIds.AddRange(testCases.Content
                .Where(t => _migrateAutotests || t.Automated == false)
                .Select(t => t.Id)
                .ToList());

            page++;

            _logger.LogInformation("Got test case ids from suite {SuiteId} " +
                                   "from {Page} page out of {TotalPages} pages", suiteId, page, totalPages);
        } while (page < totalPages);

        return testCaseIds;
    }

    public async Task<List<AllureSharedStep>> GetSharedStepsByProjectId(long projectId)
    {
        var allSharedSteps = new List<AllureSharedStep>();
        var page = 0;
        var totalPages = -1L;

        _logger.LogInformation("Getting shared step ids by project id {ProjectId}", projectId);

        do
        {
            var response =
                await _httpClient.GetAsync($"api/rs/sharedstep?projectId={projectId}&treeId=2&page={page}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get shared step ids by project id {ProjectId}. " +
                    "Status code: {StatusCode}. Response: {Response}",
                    projectId, response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception(
                    $"Failed to get shared step ids by project id {projectId}. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var sharedSteps = JsonSerializer.Deserialize<AllureSharedSteps>(content)!;
            totalPages = sharedSteps.TotalPages;

            allSharedSteps.AddRange(sharedSteps.Content);

            page++;

            _logger.LogInformation("Got shared step ids by project id {ProjectId} from {Page} " +
                                   "page out of {TotalPages} pages", projectId, page, totalPages);
        } while (page < totalPages);

        return allSharedSteps;
    }


    public async Task<AllureTestCase> GetTestCaseById(long testCaseId)
    {
        var requestUri = $"api/rs/testcase/{testCaseId}";
        return await GetGenericTcData<AllureTestCase>(requestUri, testCaseId,
            "test case", "");
    }

    public async Task<List<AllureStep>> GetSteps(long testCaseId)
    {
        var requestUri = $"api/rs/testcase/{testCaseId}/scenario";
        var steps = await GetGenericTcData<AllureSteps>(requestUri, testCaseId,
            "steps", "test case");
        return steps.Steps;
    }

    public async Task<AllureStepsInfo> GetStepsInfoByTestCaseId(long testCaseId)
    {
        var requestUri = $"api/rs/testcase/{testCaseId}/step";
        return await GetGenericTcData<AllureStepsInfo>(requestUri, testCaseId,
            "steps info", "test case");
    }

    public async Task<AllureSharedStepsInfo> GetStepsInfoBySharedStepId(long sharedStepId)
    {
        var requestUri = $"api/rs/sharedstep/{sharedStepId}/step";
        return await GetGenericTcData<AllureStepsInfo>(requestUri, sharedStepId,
            "steps info", "shared step");
    }

    public async Task<List<AllureAttachment>> GetAttachmentsByTestCaseId(long testCaseId)
    {
        var allAttachments = new List<AllureAttachment>();
        var page = 0;
        var totalPages = -1L;

        _logger.LogInformation("Getting attachments by test case id {TestCaseId}", testCaseId);

        do
        {
            var response =
                await _httpClient.GetAsync($"api/rs/testcase/attachment?testCaseId={testCaseId}&page={page}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get attachments by test case id {TestCaseId}. Status code: {StatusCode}. Response: {Response}",
                    testCaseId, response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception(
                    $"Failed to get attachments by test case id {testCaseId}. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var attachments = JsonSerializer.Deserialize<AllureAttachmentContent>(content)!;
            totalPages = attachments.TotalPages;

            allAttachments.AddRange(attachments.Content);

            page++;

            _logger.LogInformation(
                "Got attachments by test case id {TestCaseId} from {Page} page out of {TotalPages} pages", testCaseId,
                page, totalPages);
        } while (page < totalPages);

        return allAttachments;
    }

    public async Task<List<AllureAttachment>> GetAttachmentsBySharedStepId(long sharedStepId)
    {
        var allAttachments = new List<AllureAttachment>();
        var page = 0;
        var totalPages = -1L;

        _logger.LogInformation("Getting attachments by shared step id {SharedStepId}", sharedStepId);

        do
        {
            var response =
                await _httpClient.GetAsync($"api/rs/sharedstep/attachment?sharedStepId={sharedStepId}&page={page}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get attachments by shared step id {SharedStepId}. Status code: {StatusCode}. Response: {Response}",
                    sharedStepId, response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception(
                    $"Failed to get attachments by shared step id {sharedStepId}. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var attachments = JsonSerializer.Deserialize<AllureAttachmentContent>(content)!;
            totalPages = attachments.TotalPages;

            allAttachments.AddRange(attachments.Content);

            page++;

            _logger.LogInformation(
                "Got attachments by shared step id {SharedStepId} from {Page} page out of {TotalPages} pages",
                sharedStepId, page, totalPages);
        } while (page < totalPages);

        return allAttachments;
    }

    public async Task<List<AllureLink>> GetIssueLinks(long testCaseId)
    {
        var requestUri = $"api/rs/testcase/{testCaseId}/issue";
        return await GetGenericTcData<List<AllureLink>>(requestUri, testCaseId,
            "issue links", "test case");
    }

    public async Task<List<AllureRelation>> GetRelations(long testCaseId)
    {
        var requestUri = $"api/rs/testcase/{testCaseId}/relation";
        return await GetGenericTcData<List<AllureRelation>>(requestUri, testCaseId,
            "relations", "test case");
    }


    public async Task<TcCommentsSection> GetComments(long testCaseId)
    {
        var requestUri = $"api/rs/comment?testCaseId={testCaseId}&page=0&size=25";
        return await GetGenericTcData<TcCommentsSection>(requestUri, testCaseId,
            "comments", "test case");
    }

    public async Task<List<BaseEntity>> GetSuites(long projectId)
    {
        var allSuites = new List<BaseEntity>();
        var page = 0;
        var countOfSuites = 0;

        _logger.LogInformation("Getting suites by project id {Id}", projectId);

        do
        {
            var requestUri = $"api/rs/testcasetree/group?projectId={projectId}&treeId=2&page={page}";
            var suites = await GetGenericTcData<BaseEntities>(requestUri, projectId,
                "suites", "project");

            countOfSuites = suites.Content.Count;

            allSuites.AddRange(suites.Content);

            page++;

            _logger.LogInformation("Got {Count} suites by project id {Id} from {Page} page", countOfSuites, projectId, page);
        } while (countOfSuites > 0);

        return allSuites;
    }

    public async Task<byte[]> DownloadAttachmentForTestCase(long attachmentId)
    {
        _logger.LogInformation("Downloading attachment for test case with id {AttachmentId}", attachmentId);

        try
        {
            return await _httpClient.GetByteArrayAsync(
                $"api/rs/testcase/attachment/{attachmentId}/content?inline=false");
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Failed to download attachment for test case with id {AttachmentId}: {@Ex}", attachmentId,
                ex);

            return [];
        }
    }

    public async Task<byte[]> DownloadAttachmentForSharedStep(long attachmentId)
    {
        _logger.LogInformation("Downloading attachment for shared step with id {AttachmentId}", attachmentId);

        try
        {
            return await _httpClient.GetByteArrayAsync(
                $"api/rs/sharedstep/attachment/{attachmentId}/content?inline=false");
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Failed to download attachment for shared step with id {AttachmentId}: {@Ex}",
                attachmentId, ex);

            return [];
        }
    }

    public async Task<List<BaseEntity>> GetTestLayers()
    {
        _logger.LogInformation("Getting test layers");

        var response = await _httpClient.GetAsync("api/rs/testlayer");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get test layers. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get test layers. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var layers = JsonSerializer.Deserialize<BaseEntities>(content)!;

        return layers.Content.ToList();
    }

    public async Task<List<BaseEntity>> GetCustomFieldNames(long projectId)
    {
        var requestUri = $"api/rs/cf?projectId={projectId}";
        return await GetGenericTcData<List<BaseEntity>>(requestUri, projectId,
            "custom field names", "project");
    }

    public async Task<List<BaseEntity>> GetCustomFieldValues(long fieldId, long projectId)
    {
        var requestUri = $"api/rs/cfv?customFieldId={fieldId}&projectId={projectId}";
        var values = await GetGenericTcData<BaseEntities>(requestUri, fieldId,
            "custom field values", "field");
        return values.Content.ToList();
    }

    public async Task<List<AllureCustomField>> GetCustomFieldsFromTestCase(long testCaseId)
    {
        var requestUri = $"api/rs/testcase/{testCaseId}/cfv";
        return await GetGenericTcData<List<AllureCustomField>>(requestUri, testCaseId,
            "custom fields", "test case");
    }

    private async Task<T> GetGenericTcData<T>(string requestUri, long id, string logDomain, string logBaseDomain)
    {
        _logger.LogInformation("Getting {logDomain} for {logBaseDomain} with id {Id}",
            logDomain, logBaseDomain, id);

        var response = await _httpClient.GetAsync(requestUri);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get {logDomain} for {logBaseDomain} with id {Id}. " +
                "Status code: {StatusCode}. Response: {Response}",
                logDomain, logBaseDomain, id, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get {logDomain} for {logBaseDomain} with id {id}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content)!;
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
