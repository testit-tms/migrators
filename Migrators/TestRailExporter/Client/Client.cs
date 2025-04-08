using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Text;
using TestRailExporter.Models.Client;
using Microsoft.Extensions.Options;

namespace TestRailExporter.Client;

public class Client : IClient
{
    private readonly ILogger<Client> _logger;
    private readonly HttpClient _httpClient;
    private readonly AppConfig _config;
    private readonly string _projectName;
    private readonly int _limit = 100;

    public Client(ILogger<Client> logger, HttpClient httpClient, IOptions<AppConfig> config)
    {
        _config = config.Value;
        _logger = logger;
        _projectName = _config.TestRail.ProjectName;
        _httpClient = httpClient;

        InitHttpClient();
    }

    private void InitHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_config.TestRail.Url);

        var header = GetAuthHeaderBy(_config.TestRail.Login, _config.TestRail.Password);
        if (header == null)
        {
            throw new ArgumentException("Login/password is not specified");
        }
        _httpClient.DefaultRequestHeaders
            .Add("Authorization", header);
    }

    private static string? GetAuthHeaderBy(string login, string password)
    {
        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        var loginPassPair = $"{login}:{password}";
        var basicAuthenticationValue = Convert.ToBase64String(Encoding.ASCII.GetBytes(loginPassPair));
        return "Basic " + basicAuthenticationValue;
    }

    public async Task<TestRailProject> GetProject()
    {
        _logger.LogInformation("Getting project by name {Name}", _projectName);
        var offset = 0;
        var size = 0;

        do
        {
            var response = await _httpClient.GetAsync($"index.php?/api/v2/get_projects&offset={offset}&limit={_limit}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get project id. Status code: {StatusCode}. Response: {Response}",
                    response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception($"Failed to get project id. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var projects = JsonSerializer.Deserialize<TestRailProjects>(content)!;

            var project = projects.Projects.Find(p => p.Name.Equals(_projectName));

            if (project != null)
            {
                _logger.LogInformation("Got project: {@Project}", project);

                return project;
            }

            size = projects.Size;
            offset += size;
        } while (size > 0);

        _logger.LogError("Not found the project \"{Name}\"", _projectName);

        throw new Exception($"Not found the project \"{_projectName}\"");
    }

    public async Task<List<TestRailSuite>> GetSuitesByProjectId(int projectId)
    {
        _logger.LogInformation("Getting suites by project id {Id}", projectId);

        var response = await _httpClient.GetAsync($"index.php?/api/v2/get_suites/{projectId}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get suites by project id {Id}. Status code: {StatusCode}. Response: {Response}",
                projectId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get suites by project id {projectId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var suites = JsonSerializer.Deserialize<List<TestRailSuite>>(content)!;

        _logger.LogDebug("Got {Count} suites by project id {Id}: {@Suites}", suites.Count, projectId, suites);

        return suites;
    }

    public async Task<List<TestRailSection>> GetSectionsByProjectId(int projectId)
    {
        _logger.LogInformation("Getting sections by project id {Id}", projectId);

        var allSections = new List<TestRailSection>();
        var offset = 0;
        var size = 0;

        do
        {
            var response = await _httpClient.GetAsync($"index.php?/api/v2/get_sections/{projectId}&limit={_limit}&offset={offset}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get sections by project id {Id}. Status code: {StatusCode}. Response: {Response}",
                    projectId, response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception($"Failed to get sections by project id {projectId}. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var sections = JsonSerializer.Deserialize<TestRailSections>(content)!;

            allSections.AddRange(sections.Sections);
            size = sections.Size;
            offset += size;

            _logger.LogInformation("Got {Count} sections by project id {Id}", offset, projectId);
        } while (size > 0);

        _logger.LogDebug("Got {Count} sections by project id {Id}: {@Sections}", offset, projectId, allSections);

        return allSections;
    }

    public async Task<List<TestRailSection>> GetSectionsByProjectIdAndSuiteId(int projectId, int suiteId)
    {
        _logger.LogInformation("Getting sections by project id {projectId} and suite id {suiteId}", projectId, suiteId);

        var allSections = new List<TestRailSection>();
        var offset = 0;
        var size = 0;

        do
        {
            var response = await _httpClient.GetAsync($"index.php?/api/v2/get_sections/{projectId}&suite_id={suiteId}&limit={_limit}&offset={offset}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get sections by project id {projectId} and suite id {suiteId}. Status code: {StatusCode}. Response: {Response}",
                    projectId, suiteId, response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception($"Failed to get sections by project id {projectId} and suite id {suiteId}. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var sections = JsonSerializer.Deserialize<TestRailSections>(content)!;

            allSections.AddRange(sections.Sections);
            size = sections.Size;
            offset += size;

            _logger.LogInformation("Got {Count} sections by project id {projectId} and suite id {suiteId}", offset, projectId, suiteId);
        } while (size > 0);

        _logger.LogDebug(
            "Got {Count} sections by project id {projectId} and suite id {suiteId}: {@Sections}",
            offset, projectId, suiteId, allSections);

        return allSections;
    }

    public async Task<List<TestRailSharedStep>> GetSharedStepIdsByProjectId(int projectId)
    {
        _logger.LogInformation("Getting shared step ids by project id {ProjectId}", projectId);

        var allSharedSteps = new List<TestRailSharedStep>();
        var offset = 0;
        var size = 0;

        do
        {
            var response = await _httpClient.GetAsync($"index.php?/api/v2/get_shared_steps/{projectId}&limit={_limit}&offset={offset}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get shared step ids by project id {ProjectId}. Status code: {StatusCode}. Response: {Response}",
                    projectId, response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception(
                    $"Failed to get shared step ids by project id {projectId}. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var sharedSteps = JsonSerializer.Deserialize<TestRailSharedSteps>(content)!;

            allSharedSteps.AddRange(sharedSteps.SharedSteps);

            size = sharedSteps.Size;
            offset += size;

            _logger.LogInformation("Got {Count} shared step ids by project id {ProjectId}", offset, projectId);
        } while (size > 0);

        return allSharedSteps;
    }

    public async Task<List<TestRailCase>> GetTestCaseIdsByProjectIdAndSectionId(int projectId, int sectionId)
    {
        _logger.LogInformation("Getting test case ids by project id {ProjectId} and section id {SectionId}", projectId, sectionId);

        var allTestCases = new List<TestRailCase>();
        var offset = 0;
        var size = 0;

        do
        {
            var response = await _httpClient.GetAsync($"index.php?/api/v2/get_cases/{projectId}&section_id={sectionId}&limit={_limit}&offset={offset}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get test case ids by project id {ProjectId} and section id {SectionId}. Status code: {StatusCode}. Response: {Response}",
                    projectId, sectionId, response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception(
                    $"Failed to get test case ids by project id {projectId} and section id {sectionId}. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var testCases = JsonSerializer.Deserialize<TestRailCases>(content)!;

            allTestCases.AddRange(testCases.Cases);

            size = testCases.Size;
            offset += size;

            _logger.LogInformation("Got {Count} test case ids by project id {ProjectId} and section id {SectionId}", offset, projectId, sectionId);
        } while (size > 0);

        return allTestCases;
    }

    public async Task<List<TestRailCase>> GetTestCaseIdsByProjectIdAndSuiteIdAndSectionId(int projectId, int suiteId, int sectionId)
    {
        _logger.LogInformation(
            "Getting test case ids by project id {ProjectId} and suite id {SuiteId} and section id {SectionId}",
            projectId, suiteId, sectionId);

        var allTestCases = new List<TestRailCase>();
        var offset = 0;
        var size = 0;

        do
        {
            var response = await _httpClient.GetAsync($"index.php?/api/v2/get_cases/{projectId}&suite_id={suiteId}&section_id={sectionId}&limit={_limit}&offset={offset}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get test case ids by project id {ProjectId} and suite id {SuiteId} and section id {SectionId}. Status code: {StatusCode}. Response: {Response}",
                    projectId, suiteId, sectionId, response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception(
                    $"Failed to get test case ids by project id {projectId} and suite id {suiteId} and section id {sectionId}. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var testCases = JsonSerializer.Deserialize<TestRailCases>(content)!;

            allTestCases.AddRange(testCases.Cases);

            size = testCases.Size;
            offset += size;

            _logger.LogInformation(
                "Got {Count} test case ids by project id {ProjectId} and suite id {SuiteId} and section id {SectionId}",
                offset, projectId, suiteId, sectionId);
        } while (size > 0);

        return allTestCases;
    }

    public async Task<List<TestRailAttachment>> GetAttachmentsByTestCaseId(int testCaseId)
    {
        _logger.LogInformation("Getting attachments by test case id {CaseId}", testCaseId);
        var response = await _httpClient.GetAsync($"index.php?/api/v2/get_attachments_for_case/{testCaseId}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get attachments by test case id {CaseId}. Status code: {StatusCode}. Response: {Response}",
                testCaseId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception(
                $"Failed to get attachments by test case id {testCaseId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var attachments = JsonSerializer.Deserialize<TestRailAttachments>(content)!;

        _logger.LogDebug(
            "Got {Count} attachments by test case id {CaseId}: {@Attachments}",
            attachments.Attachments.Count, testCaseId, attachments.Attachments);

        return attachments.Attachments;
    }

    public async Task<byte[]> GetAttachmentById(int attachmentId)
    {
        _logger.LogInformation("Downloading attachment by id {AttachmentId}", attachmentId);

        try
        {
            return await _httpClient.GetByteArrayAsync($"index.php?/api/v2/get_attachment/{attachmentId}");
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Failed to download attachment by id {AttachmentId}: {@Ex}", attachmentId, ex);

            return [];
        }
    }
}
