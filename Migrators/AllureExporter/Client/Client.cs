using System.Net.Http.Headers;
using System.Text.Json;
using AllureExporter.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;

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
        var testCaseIds = new List<int>();
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

            testCaseIds.AddRange(testCases.Content
                    .Where(t => _migrateAutotests || t.Automated == false)
                    .Select(t => t.Id)
                    .ToList());

            page++;

            _logger.LogInformation("Got test case ids from main suite from {Page} page out of {TotalPages} pages", page, testCases.TotalPages);
        } while (page < totalPages);

        return testCaseIds;
    }

    public async Task<List<int>> GetTestCaseIdsFromSuite(int projectId, int suiteId)
    {
        var testCaseIds = new List<int>();
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

            testCaseIds.AddRange(testCases.Content
                    .Where(t => _migrateAutotests || t.Automated == false)
                    .Select(t => t.Id)
                    .ToList());

            page++;

            _logger.LogInformation("Got test case ids from suite {SuiteId} from {Page} page out of {TotalPages} pages", suiteId, page, totalPages);
        } while (page < totalPages);

        return testCaseIds;
    }

    public async Task<List<AllureSharedStep>> GetSharedStepsByProjectId(int projectId)
    {
        var allSharedSteps = new List<AllureSharedStep>();
        var page = 0;
        var totalPages = -1;

        _logger.LogInformation("Getting shared step ids by project id {ProjectId}", projectId);

        do
        {
            var response =
            await _httpClient.GetAsync($"api/rs/sharedstep?projectId={projectId}&treeId=2&page={page}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get shared step ids by project id {ProjectId}. Status code: {StatusCode}. Response: {Response}",
                    projectId, response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception(
                    $"Failed to get shared step ids by project id {projectId}. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var sharedSteps = JsonSerializer.Deserialize<AllureSharedSteps>(content);
            totalPages = sharedSteps.TotalPages;

            allSharedSteps.AddRange(sharedSteps.Content);

            page++;

            _logger.LogInformation("Got shared step ids by project id {ProjectId} from {Page} page out of {TotalPages} pages", projectId, page, totalPages);
        } while (page < totalPages);

        return allSharedSteps;
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

    public async Task<AllureStepsInfo> GetStepsInfoByTestCaseId(int testCaseId)
    {
        _logger.LogInformation("Getting steps info by test case id {TestCaseId}", testCaseId);

        var response = await _httpClient.GetAsync($"api/rs/testcase/{testCaseId}/step");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get steps info by test case id {TestCaseId}. Status code: {StatusCode}. Response: {Response}",
                testCaseId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get steps info by test case id {testCaseId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var stepsInfo = JsonSerializer.Deserialize<AllureStepsInfo>(content);

        return stepsInfo;
    }

    public async Task<AllureSharedStepsInfo> GetStepsInfoBySharedStepId(int sharedStepId)
    {
        _logger.LogInformation("Getting steps info by shared step id {SharedStepId}", sharedStepId);

        var response = await _httpClient.GetAsync($"api/rs/sharedstep/{sharedStepId}/step");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get steps info by shared step id {SharedStepId}. Status code: {StatusCode}. Response: {Response}",
                sharedStepId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get steps info by shared step id {sharedStepId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var sharedStepsInfo = JsonSerializer.Deserialize<AllureSharedStepsInfo>(content);

        return sharedStepsInfo;
    }

    public async Task<List<AllureAttachment>> GetAttachmentsByTestCaseId(int testCaseId)
    {
        var allAttachments = new List<AllureAttachment>();
        var page = 0;
        var totalPages = -1;

        _logger.LogInformation("Getting attachments by test case id {TestCaseId}", testCaseId);

        do
        {
            var response = await _httpClient.GetAsync($"api/rs/testcase/attachment?testCaseId={testCaseId}&page={page}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get attachments by test case id {TestCaseId}. Status code: {StatusCode}. Response: {Response}",
                    testCaseId, response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception(
                    $"Failed to get attachments by test case id {testCaseId}. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var attachments = JsonSerializer.Deserialize<AllureAttachmentContent>(content);
            totalPages = attachments.TotalPages;

            allAttachments.AddRange(attachments.Content);

            page++;

            _logger.LogInformation("Got attachments by test case id {TestCaseId} from {Page} page out of {TotalPages} pages", testCaseId, page, totalPages);
        } while (page < totalPages);

        return allAttachments;
    }

    public async Task<List<AllureAttachment>> GetAttachmentsBySharedStepId(int sharedStepId)
    {
        var allAttachments = new List<AllureAttachment>();
        var page = 0;
        var totalPages = -1;

        _logger.LogInformation("Getting attachments by shared step id {SharedStepId}", sharedStepId);

        do
        {
            var response = await _httpClient.GetAsync($"api/rs/sharedstep/attachment?sharedStepId={sharedStepId}&page={page}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get attachments by shared step id {SharedStepId}. Status code: {StatusCode}. Response: {Response}",
                    sharedStepId, response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception(
                    $"Failed to get attachments by shared step id {sharedStepId}. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var attachments = JsonSerializer.Deserialize<AllureAttachmentContent>(content);
            totalPages = attachments.TotalPages;

            allAttachments.AddRange(attachments.Content);

            page++;

            _logger.LogInformation("Got attachments by shared step id {SharedStepId} from {Page} page out of {TotalPages} pages", sharedStepId, page, totalPages);
        } while (page < totalPages);

        return allAttachments;
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

    public async Task<byte[]> DownloadAttachmentForTestCase(int attachmentId)
    {
        _logger.LogInformation("Downloading attachment for test case with id {AttachmentId}", attachmentId);

        return await _httpClient.GetByteArrayAsync($"api/rs/testcase/attachment/{attachmentId}/content?inline=false");
    }

    public async Task<byte[]> DownloadAttachmentForSharedStep(int attachmentId)
    {
        _logger.LogInformation("Downloading attachment for shared step with id {AttachmentId}", attachmentId);

        return await _httpClient.GetByteArrayAsync($"api/rs/sharedstep/attachment/{attachmentId}/content?inline=false");
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
