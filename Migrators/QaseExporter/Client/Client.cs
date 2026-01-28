using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QaseExporter.Models;

namespace QaseExporter.Client;

public class Client : IClient
{
    private readonly ILogger<Client> _logger;
    private readonly HttpClient _httpClient;
    private readonly HttpClient _appClient;
    private readonly string _projectKey;

    public Client(ILogger<Client> logger, HttpClient httpClient, HttpClient appClient, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;
        _appClient = appClient;

        var section = configuration.GetSection("qase");
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

        _projectKey = projectKey;

        _httpClient.BaseAddress = new Uri(CorrectBaseAddress(url));
        _httpClient.DefaultRequestHeaders.Add("Token", token);

        var appUrl = section["appUrl"];
        if (!string.IsNullOrEmpty(appUrl))
        {
            var cookie = section["cookie"];
            if (string.IsNullOrEmpty(cookie))
            {
                throw new ArgumentException("Cookie is not specified");
            }

            var xXsrfToken = section["xXsrfToken"];
            if (string.IsNullOrEmpty(xXsrfToken))
            {
                throw new ArgumentException("xXsrfToken is not specified");
            }

            _appClient.BaseAddress = new Uri(CorrectBaseAddress(appUrl));
            _appClient.DefaultRequestHeaders.Add("Cookie", cookie);
            _appClient.DefaultRequestHeaders.Add("x-xsrf-token", xXsrfToken);
        }
    }

    public async Task<QaseProject> GetProject()
    {
        _logger.LogInformation("Getting project by key {ProjectKey}", _projectKey);

        var response = await _httpClient.GetAsync($"v1/project/{_projectKey}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get project by key {ProjectKey}. Status code: {StatusCode}. Response: {Response}",
                _projectKey, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get project by key {_projectKey}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var projectData = JsonSerializer.Deserialize<QaseProjectData>(content)!;

        _logger.LogDebug("Found project {@Project}", projectData.Project);

        return projectData.Project;
    }

    public async Task<List<QaseSuite>> GetSuites()
    {
        _logger.LogInformation("Getting suites by project key {Key}", _projectKey);

        var allSuites = new List<QaseSuite>();
        var startAt = 0;
        var maxResults = 100;
        var countOfSuites = 0;
        var total = 0;

        do
        {
            var response = await _httpClient.GetAsync($"v1/suite/{_projectKey}?limit={maxResults}&offset={startAt}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get suites by project key {Key}. Status code: {StatusCode}. Response: {Response}",
                    _projectKey, response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception($"Failed to get suites by project key {_projectKey}. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var suitesData = JsonSerializer.Deserialize<QaseSuitesData>(content)!;

            if (suitesData.SuitesData.Count > 0)
            {
                _logger.LogDebug("Got suites {@Suites}", suitesData.SuitesData.Suites);

                allSuites.AddRange(suitesData.SuitesData.Suites);
                startAt += maxResults;
                total = suitesData.SuitesData.Filtered;
                countOfSuites += suitesData.SuitesData.Count;
            }
            else
            {
                startAt = -1;
            }

            _logger.LogInformation("Got {Count} out of {Total} suites", countOfSuites, total);
        } while (countOfSuites < total && startAt >= 0);

        return allSuites;
    }

    public async Task<List<QaseTestCase>> GetTestCasesBySuiteId(int suiteId)
    {
        _logger.LogInformation("Getting test cases by suite id {Id}", suiteId);

        var allTestCases = new List<QaseTestCase>();
        var startAt = 0;
        var maxResults = 100;
        var countOfTests = 0;
        var total = 0;

        do
        {
            var response = await _httpClient.GetAsync($"v1/case/{_projectKey}?limit={maxResults}&offset={startAt}&suite_id={suiteId}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get test cases by suite id {Id}. Status code: {StatusCode}. Response: {Response}",
                    suiteId, response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception($"Failed to get test cases by suite id {suiteId}. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var qaseCasesData = JsonSerializer.Deserialize<QaseCasesData>(content)!;

            if (qaseCasesData.CasesData.Count > 0)
            {
                _logger.LogDebug("Got test cases {@Cases}", qaseCasesData.CasesData.Cases);

                allTestCases.AddRange(qaseCasesData.CasesData.Cases);
                startAt += maxResults;
                total = qaseCasesData.CasesData.Filtered;
                countOfTests += qaseCasesData.CasesData.Count;
            }
            else
            {
                startAt = -1;
            }

            _logger.LogInformation("Got {Count} out of {Total} test cases", countOfTests, total);
        } while (countOfTests < total && startAt >= 0);

        return allTestCases;
    }

    public async Task<List<QaseSharedStep>> GetSharedSteps()
    {
        _logger.LogInformation("Getting shared steps");

        var allSharedSteps = new List<QaseSharedStep>();
        var startAt = 0;
        var maxResults = 100;
        var countOfSharedSteps = 0;
        var total = 0;

        do
        {
            var response = await _httpClient.GetAsync($"v1/shared_step/{_projectKey}?limit={maxResults}&offset={startAt}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get shared steps. Status code: {StatusCode}. Response: {Response}",
                    response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception($"Failed to get shared steps. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var sharedStepData = JsonSerializer.Deserialize<SharedStepData>(content)!;

            if (sharedStepData.SharedStepsData.Count > 0)
            {
                _logger.LogDebug("Got shared steps {@SharedSteps}", sharedStepData.SharedStepsData.SharedSteps);

                allSharedSteps.AddRange(sharedStepData.SharedStepsData.SharedSteps);
                startAt += maxResults;
                total = sharedStepData.SharedStepsData.Filtered;
                countOfSharedSteps += sharedStepData.SharedStepsData.Count;
            }
            else
            {
                startAt = -1;
            }

            _logger.LogInformation("Got {Count} out of {Total} shared steps", countOfSharedSteps, total);
        } while (countOfSharedSteps < total && startAt >= 0);

        return allSharedSteps;
    }

    public async Task<List<QaseCustomField>> GetCustomFields()
    {
        _logger.LogInformation("Getting custom fields");

        var allCustomFields = new List<QaseCustomField>();
        var startAt = 0;
        var maxResults = 100;
        var countOfFields = 0;
        var total = 0;

        do
        {
            var response = await _httpClient.GetAsync($"v1/custom_field?limit={maxResults}&offset={startAt}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get custom fields. Status code: {StatusCode}. Response: {Response}",
                    response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception($"Failed to get custom fields. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var fieldsData = JsonSerializer.Deserialize<QaseFieldsData>(content)!;

            if (fieldsData.FieldsData.Count > 0)
            {
                _logger.LogDebug("Got custom fields {@Fields}", fieldsData.FieldsData.Fields);

                allCustomFields.AddRange(fieldsData.FieldsData.Fields);
                startAt += maxResults;
                total = fieldsData.FieldsData.Filtered;
                countOfFields += fieldsData.FieldsData.Count;
            }
            else
            {
                startAt = -1;
            }

            _logger.LogInformation("Got {Count} out of {Total} custom fields", countOfFields, total);
        } while (countOfFields < total && startAt >= 0);

        return allCustomFields;
    }

    public async Task<List<QaseSystemField>> GetSystemFields()
    {
        _logger.LogInformation("Getting system fields");

        var response = await _httpClient.GetAsync($"v1/system_field");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get system fields. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get system fields. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var systemFieldsData = JsonSerializer.Deserialize<QaseSysFieldsData>(content)!;

        _logger.LogDebug("Got system fields: {@Fields}", systemFieldsData.Fields);

        return systemFieldsData.Fields;
    }

    public async Task<QaseAuthor> GetAuthor(int id)
    {
        _logger.LogInformation("Getting author by id {Id}", id);

        var response = await _httpClient.GetAsync($"v1/author/{id}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get author. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get author. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var author = JsonSerializer.Deserialize<QaseAuthor>(content)!;

        _logger.LogDebug("Got author: {@Author}", author);

        return author;
    }

    public async Task<List<QaseTestRun>> GetTestRuns()
    {
        _logger.LogInformation("Getting test runs by project id {Id}", _projectKey);

        var allTestRuns = new List<QaseTestRun>();
        var startAt = 0;
        var maxResults = 100;
        var countOfFields = 0;
        var total = 0;

        do
        {
            var response = await _httpClient.GetAsync(
                $"v1/run/{_projectKey}?limit={maxResults}&offset={startAt}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get test runs. Status code: {StatusCode}. Response: {Response}",
                    response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception($"Failed to get test runs. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var testRunResponse = JsonSerializer.Deserialize<QaseTestRunResponse>(content)!;

            if (testRunResponse.Result.Count > 0)
            {
                _logger.LogDebug("Got test runs {@Runs}", testRunResponse.Result.Entities);

                allTestRuns.AddRange(testRunResponse.Result.Entities);
                startAt += maxResults;
                total = testRunResponse.Result.Filtered;
                countOfFields += testRunResponse.Result.Count;
            }
            else
            {
                startAt = -1;
            }

            _logger.LogInformation("Got {Count} out of {Total} test runs", countOfFields, total);
        } while (countOfFields < total && startAt >= 0);

        return allTestRuns;
    }

    public async Task<string?> GetTestRunHash(int id)
    {
        if (_appClient.BaseAddress == null)
        {
            _logger.LogDebug("Skip get test run hash. AppClient is not initialized");

            return null;
        }

        _logger.LogInformation("Getting test run hash by test run id {Id}", id);

        var response = await _appClient.GetAsync($"v1/project/TP/run/{id}/dashboard/info");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get test run hash. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get test run hash. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var testRunInfo = JsonSerializer.Deserialize<QaseTestRunInfo>(content)!;

        _logger.LogDebug("Got test run: {@TestRun}", testRunInfo);

        return testRunInfo.Hash;
    }

    public async Task<Dictionary<string, QaseCaseStat>> GetTestResultStats(string testRunHash)
    {
        if (_appClient.BaseAddress == null)
        {
            _logger.LogDebug("Skip get test result stats. AppClient is not initialized");

            return new();
        }

        _logger.LogInformation("Getting test result stats by test run hash {Hash}", testRunHash);

        var response = await _appClient.GetAsync(
            $"v1/project/TP/run/{testRunHash}/dashboard/case-stats");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get test run hash. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get test run hash. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var qaseStats = JsonSerializer.Deserialize<QaseCaseStatsResponse>(content)!;

        _logger.LogDebug("Got test result stats: {@Hashes}", qaseStats.StatMap);

        return qaseStats.StatMap;
    }

    public async Task<QaseTestResult?> GetTestResult(string testRunHash, string testResultHash)
    {
        if (_appClient.BaseAddress == null)
        {
            _logger.LogDebug("Skip get test result. AppClient is not initialized");

            return null;
        }

        _logger.LogInformation("Getting test result by test run hash {TestRunHash} and test result hash {TestResultHash}",
            testRunHash, testResultHash);

        var response = await _appClient.GetAsync(
            $"v1/project/TP/run/{testRunHash}/dashboard/cases?ids={testResultHash}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get test result. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get test result. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var testResults = JsonSerializer.Deserialize<List<QaseTestResult>>(content)!;

        _logger.LogDebug("Got test result: {@Author}", testResults.First());

        return testResults.First();
    }

    public async Task<QaseTestPlan> GetTestPlan(string id)
    {
        _logger.LogInformation("Getting test plan by id {Id}", id);

        var response = await _httpClient.GetAsync($"v1/plan/{_projectKey}/{id}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get test plan. Status code: {StatusCode}. Response: {Response}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new Exception($"Failed to get test plan. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var testPlanResponse = JsonSerializer.Deserialize<QaseTestPlanResponse>(content)!;

        _logger.LogDebug("Got test plan: {@Plan}", testPlanResponse.Plan);

        return testPlanResponse.Plan;
    }

    public async Task<byte[]> DownloadAttachment(string url)
    {
        return await _httpClient.GetByteArrayAsync(url);
    }

    public string GetProjectKey()
    {
        return _projectKey;
    }

    private string CorrectBaseAddress(string url)
    {
        if (url.EndsWith('/'))
        {
            return url;
        }
        return url + '/';
    }

    public async Task<string?> GetComments(int id)
    {
        if (_appClient.BaseAddress == null)
        {
            _logger.LogDebug("Skip get comments. AppClient is not initialized");

            return null;
        }

        _logger.LogInformation("Getting comments by test case id {Id}", id);

        var allComments = new List<string>();
        var startAt = 0;
        var maxResults = 100;
        var countOfComments = 0;
        var total = 0;

        do
        {
            var response = await _appClient.GetAsync(
                $"v1/projects/{_projectKey}/cases/{id}/comments?limit={maxResults}&offset={startAt}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get comments. Status code: {StatusCode}. Response: {Response}",
                    response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new Exception($"Failed to get comments. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var commentResponse = JsonSerializer.Deserialize<QaseCommentResponse>(content)!;

            if (commentResponse.Comments.Count > 0)
            {
                _logger.LogDebug("Got {Count} comments", commentResponse.Comments.Count);

                allComments.AddRange(commentResponse.Comments);
                startAt += maxResults;
                total = commentResponse.Total;
                countOfComments += commentResponse.Comments.Count;
            }
            else
            {
                startAt = -1;
            }

            _logger.LogInformation("Got {Count} out of {Total} comments", countOfComments, total);
        } while (countOfComments < total && startAt >= 0);

        return string.Join("\n", allComments);
    }
}
