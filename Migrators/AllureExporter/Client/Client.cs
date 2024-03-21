using System.Net.Http.Headers;
using System.Text.Json;
using AllureExporter.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AllureExporter.Client;

public class Client : IClient
{
    private readonly ILogger<Client> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _projectName;
    private readonly bool _migrateAutotests;

    public Client(ILogger<Client> logger, IConfiguration configuration)
    {
        _logger = logger;

        var section = configuration.GetSection("allure");
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

        var migrateAutotests = section["migrateAutotests"];
        _migrateAutotests = !string.IsNullOrEmpty(migrateAutotests) && bool.Parse(migrateAutotests);

        _projectName = projectName;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(url);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Api-Token", token);
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


    public async Task<List<int>> GetTestCaseIdsFromMainSuite(int projectId)
    {
        var allTestCases = new List<int>();
        var page = 0;
        var totalPages = -1;

        _logger.LogInformation("Getting test case ids from main suite");

        do
        {
            var response = await _httpClient.GetAsync($"api/rs/testcasetree/leaf?projectId={projectId}&treeId=2&page={page}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get test case ids from main suite. Status code: {StatusCode}. Response: {Response}",
                    response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception($"Failed to get test case ids from main suite. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var testCases = JsonSerializer.Deserialize<AllureTestCases>(content);
            totalPages = testCases.TotalPages;

            allTestCases.AddRange(testCases.Content
                    .Where(t => _migrateAutotests || t.Automated == false)
                    .Select(t => t.Id)
                    .ToList());

            page++;

            _logger.LogInformation("Got test case ids from main suite from {Page} page out of {TotalPages} pages", page, testCases.TotalPages);
        } while (page < totalPages);

        return allTestCases;
    }

    public async Task<List<int>> GetTestCaseIdsFromSuite(int projectId, int suiteId)
    {
        var allTestCases = new List<int>();
        var page = 0;
        var totalPages = -1;

        _logger.LogInformation("Getting test case ids from suite {SuiteId}", suiteId);

        do
        {
            var response =
            await _httpClient.GetAsync($"api/rs/testcasetree/leaf?projectId={projectId}&treeId=2&path={suiteId}&page={page}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get test case ids from suite {SuiteId}. Status code: {StatusCode}. Response: {Response}",
                    suiteId, response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception(
                    $"Failed to get test case ids from suite {suiteId}. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var testCases = JsonSerializer.Deserialize<AllureTestCases>(content);
            totalPages = testCases.TotalPages;

            allTestCases.AddRange(testCases.Content
                    .Where(t => _migrateAutotests || t.Automated == false)
                    .Select(t => t.Id)
                    .ToList());

            page++;

            _logger.LogInformation("Got test case ids from suite {SuiteId} from {Page} page out of {TotalPages} pages", suiteId, page, totalPages);
        } while (page < totalPages);

        return allTestCases;
    }

    public async Task<AllureTestCase> GetTestCaseById(int testCaseId)
    {
        _logger.LogInformation("Getting test case with id {TestCaseId}", testCaseId);

        var response = await _httpClient.GetAsync($"api/rs/testcase/{testCaseId}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get test case with id {TestCaseId}. Status code: {StatusCode}. Response: {Response}",
                testCaseId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get test case with id {testCaseId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AllureTestCase>(content);
    }

    public async Task<List<AllureStep>> GetSteps(int testCaseId)
    {
        _logger.LogInformation("Getting steps for test case with id {TestCaseId}", testCaseId);

        var response = await _httpClient.GetAsync($"api/rs/testcase/{testCaseId}/scenario");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get steps for test case with id {TestCaseId}. Status code: {StatusCode}. Response: {Response}",
                testCaseId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get steps for test case with id {testCaseId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var steps = JsonSerializer.Deserialize<AllureSteps>(content);

        return steps.Steps;
    }

    public async Task<List<AllureAttachment>> GetAttachments(int testCaseId)
    {
        _logger.LogInformation("Getting attachments for test case with id {TestCaseId}", testCaseId);

        var response = await _httpClient.GetAsync($"api/rs/testcase/attachment?testCaseId={testCaseId}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get attachments for test case with id {TestCaseId}. Status code: {StatusCode}. Response: {Response}",
                testCaseId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get attachments for test case with id {testCaseId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var attachments = JsonSerializer.Deserialize<AllureAttachmentContent>(content);

        return attachments.Content;
    }

    public async Task<List<AllureLink>> GetLinks(int testCaseId)
    {
        _logger.LogInformation("Getting links for test case with id {TestCaseId}", testCaseId);

        var response = await _httpClient.GetAsync($"api/rs/testcase/{testCaseId}/issue");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get links for test case with id {TestCaseId}. Status code: {StatusCode}. Response: {Response}",
                testCaseId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get links for test case with id {testCaseId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<AllureLink>>(content);
    }

    public async Task<List<BaseEntity>> GetSuites(int projectId)
    {
        _logger.LogInformation("Getting suites for project with id {ProjectId}", projectId);

        var response = await _httpClient.GetAsync($"api/rs/testcasetree/group?projectId={projectId}&treeId=2");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get suites for project with id {ProjectId}. Status code: {StatusCode}. Response: {Response}",
                projectId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get suites for project with id {projectId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var suites = JsonSerializer.Deserialize<BaseEntities>(content);

        return suites.Content.ToList();
    }

    public async Task<byte[]> DownloadAttachment(int attachmentId)
    {
        _logger.LogInformation("Downloading attachment with id {AttachmentId}", attachmentId);

        return await _httpClient.GetByteArrayAsync($"api/rs/testcase/attachment/{attachmentId}/content?inline=false");
    }

    public async Task<List<BaseEntity>> GetTestLayers()
    {
        _logger.LogInformation("Getting test layers");

        var response = await _httpClient.GetAsync($"api/rs/testlayer");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get test layers. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get test layers. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var layers = JsonSerializer.Deserialize<BaseEntities>(content);

        return layers.Content.ToList();
    }

    public async Task<List<BaseEntity>> GetCustomFieldNames(int projectId)
    {
        _logger.LogInformation("Get custom field names for project with id {ProjectId}", projectId);

        var response = await _httpClient.GetAsync($"api/rs/cf?projectId={projectId}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get custom field names for project with id {ProjectId}. Status code: {StatusCode}. Response: {Response}",
                projectId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get custom field names for project with id {projectId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var customFields = JsonSerializer.Deserialize<List<BaseEntity>>(content);

        return customFields;
    }

    public async Task<List<BaseEntity>> GetCustomFieldValues(int fieldId)
    {
        _logger.LogInformation("Get custom field values for field with id {FieldId}", fieldId);

        var response = await _httpClient.GetAsync($"api/rs/cfv?customFieldId={fieldId}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get custom field values for field with id {FieldId}. Status code: {StatusCode}. Response: {Response}",
                fieldId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get custom field values for field with id {fieldId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var values = JsonSerializer.Deserialize<BaseEntities>(content);

        return values.Content.ToList();
    }

    public async Task<List<AllureCustomField>> GetCustomFieldsFromTestCase(int testCaseId)
    {
        _logger.LogInformation("Get custom fields from test case with id {TestCaseId}", testCaseId);

        var response = await _httpClient.GetAsync($"api/rs/testcase/{testCaseId}/cfv");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get custom fields from test case with id {TestCaseId}. Status code: {StatusCode}. Response: {Response}",
                testCaseId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get custom fields from test case with id {testCaseId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var customFields = JsonSerializer.Deserialize<List<AllureCustomField>>(content);

        return customFields;
    }
}
