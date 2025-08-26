using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using ZephyrScaleServerExporter.Client.Exceptions;
using ZephyrScaleServerExporter.Models;
using ZephyrScaleServerExporter.Models.Attachment;
using ZephyrScaleServerExporter.Models.Client;
using ZephyrScaleServerExporter.Models.TestCases;
using ZephyrScaleServerExporter.Services;
using nsJson = Newtonsoft.Json;


namespace ZephyrScaleServerExporter.Client;

public class Client : IClient
{
    private readonly ILogger<Client> _logger;
    private readonly HttpClient _httpClient;
    private readonly ITestCaseClient _testCaseClient;
    private readonly AppConfig _config;
    private readonly HttpClient _confluenceHttpClient;
    private readonly string _projectKey;
    private readonly Uri _baseUrl;
    private readonly Uri _confluenceBaseUrl;
    private bool _unauthorizedConfluence;
    private readonly IDetailedLogService _detailedLogService;

    public Client(ILogger<Client> logger,
        HttpClient httpClient,
        IOptions<AppConfig> config,
        ITestCaseClient testCaseClient,
        HttpClient confluenceHttpClient,
        IDetailedLogService detailedLogService)
    {
        _testCaseClient = testCaseClient;
        _config = config.Value;
        _logger = logger;
        _projectKey = _config.Zephyr.ProjectKey;
        _baseUrl = new Uri(_config.Zephyr.Url);
        _confluenceBaseUrl = new Uri(_config.Zephyr.Confluence);
        _detailedLogService = detailedLogService;

        _httpClient = httpClient;
        _confluenceHttpClient = confluenceHttpClient;

        InitHttpClient();
        InitConfluenceHttpClient();
    }

    private void InitHttpClient()
    {
        _httpClient.Timeout = TimeSpan.FromSeconds(1000);

        var header = GetAuthHeaderBy(_config.Zephyr.Token,
            _config.Zephyr.Login, _config.Zephyr.Password);
        if (header == null)
        {
            throw new ArgumentException("Token or login/password is not specified");
        }
        _httpClient.DefaultRequestHeaders
            .Add("Authorization", header);
    }

    private void InitConfluenceHttpClient()
    {
        _confluenceHttpClient.Timeout = TimeSpan.FromSeconds(1000);

        var header = GetAuthHeaderBy(_config.Zephyr.ConfluenceToken,
            _config.Zephyr.ConfluenceLogin, _config.Zephyr.ConfluencePassword);
        if (header == null)
        {
            throw new ArgumentException("Confluence Token or login/password is not specified");
        }
        _confluenceHttpClient.DefaultRequestHeaders
            .Add("Authorization", header);
    }


    private static string? GetAuthHeaderBy(string token, string login, string password)
    {
        if (!string.IsNullOrEmpty(token))
        {
            return "Bearer " + token;
        }

        if (!string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(password))
        {
            var loginPassPair = $"{login}:{password}";
            var basicAuthenticationValue = Convert.ToBase64String(Encoding.ASCII.GetBytes(loginPassPair));
            return "Basic " + basicAuthenticationValue;
        }
        return null;
    }

    public async Task<ZephyrProject> GetProject()
    {
        _logger.LogInformation("Getting project by key {ProjectKey}", _projectKey);

        var response = await GetAsync($"/rest/api/2/project/{_projectKey}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get project by key {ProjectKey}. Status code: {StatusCode}. Response: {Response}",
                _projectKey, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new ApiException($"Failed to get project by key {_projectKey}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var project = JsonSerializer.Deserialize<ZephyrProject>(content)!;

        _detailedLogService.LogDebug("Found project {@ProjectId}", project);

        return project;
    }

    public async Task<List<ZephyrStatus>> GetStatuses(string projectId)
    {
        _logger.LogInformation("Getting statuses by project id {Id}", projectId);

        var response = await GetAsync($"rest/tests/1.0/testcasestatus?projectId={projectId}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get statuses by project id {Id}. Status code: {StatusCode}. Response: {Response}",
                projectId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new ApiException($"Failed to get statuses by project id {projectId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var statuses = JsonSerializer.Deserialize<List<ZephyrStatus>>(content)!;

        _detailedLogService.LogDebug("Got statuses {@Statuses}", statuses);

        return statuses;
    }

    public async Task<List<ZephyrCustomFieldForTestCase>> GetCustomFieldsForTestCases(string projectId)
    {
        _logger.LogInformation("Getting custom fields by project id {Id}", projectId);

        var response = await GetAsync($"/rest/tests/1.0/project/{projectId}/customfields/testcase");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get custom fields by project id {Id}. Status code: {StatusCode}. Response: {Response}",
                projectId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new ApiException($"Failed to get custom fields by project id {projectId}. " +
                                           $"Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var customFields = JsonSerializer
            .Deserialize<List<ZephyrCustomFieldForTestCase>>(content)!
            .Where(c => c.Archived == false)
            .Select(c => c).ToList();

        _detailedLogService.LogDebug("Got custom fields {@CustomFields}", customFields);

        return customFields;
    }

    public async Task<List<ZephyrTestCase>> GetTestCasesWithFilter(int startAt, int maxResults, string statuses, string filter)
    {
        var reqString = $"/rest/tests/1.0/testcase/search?maxResults={maxResults}&startAt={startAt}&query=testCase.projectKey = \"{_projectKey}\" AND testCase.statusName IN ({statuses}) {filter}";
        return await _testCaseClient.GetTestCasesCoreHandlerNewApi(_httpClient, _projectKey, FromBase(reqString));
    }

    public async Task<List<ZephyrTestCase>> GetTestCases(int startAt, int maxResults, string statuses)
    {
        var reqString = $"/rest/atm/1.0/testcase/search?maxResults={maxResults}&startAt={startAt}&query=projectKey = \"{_projectKey}\" AND status IN ({statuses})";
        return await _testCaseClient.GetTestCasesCoreHandler(_httpClient, _projectKey, FromBase(reqString));
    }

    public async Task<List<ZephyrTestCase>> GetTestCasesArchived(int startAt, int maxResults, string statuses)
    {
        var reqString = $"rest/tests/1.0/testcase/search?query=testCase.projectKey=\"{_projectKey}\"+AND+testCase.statusName+IN+({statuses})&startAt={startAt}&maxResults={maxResults}&archived=true";
        return await _testCaseClient.GetTestCasesCoreHandlerNewApi(_httpClient, _projectKey, reqString);
    }

    [Obsolete("not used")]
    public async Task<List<ZephyrTestCaseRoot>> GetTestCasesNew(string statuses)
    {
        _logger.LogInformation("Getting test cases by project key {Key}", _projectKey);

        var allTestCases = new List<ZephyrTestCaseRoot>();
        var startAt = 0;
        var maxResults = 100;
        var countOfTests = 0;

        do
        {
            var reqString = $"/rest/tests/1.0/testcase/search?maxResults={maxResults}&startAt={startAt}&query=testCase.projectKey = \"{_projectKey}\" AND testCase.statusName IN ({statuses})";
            _detailedLogService.LogDebug("reqString: {ReqString}", reqString);
            var response = await GetAsync(reqString);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get test cases by project key {Key}. Status code: {StatusCode}. Response: {Response}",
                    _projectKey, response.StatusCode, await response.Content.ReadAsStringAsync());

                throw new ApiException($"Failed to get test cases by project key {_projectKey}. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();

            var wrapper = JsonSerializer.Deserialize<TestCaseResponseWrapper>(content)!;

            var testCases = wrapper.Results.ToList();

            if (testCases.Any())
            {
                _detailedLogService.LogDebug("Got test cases {@TestCases}", testCases);

                allTestCases.AddRange(testCases);
                startAt += maxResults;
                countOfTests += testCases.Count;

                _logger.LogInformation("Got {Count} test cases", countOfTests);

            }
            else
            {
                startAt = -1;
            }
        } while (startAt >= 0);

        return allTestCases;
    }



    public async Task<ZephyrTestCase> GetTestCase(string testCaseKey)
    {
        _logger.LogInformation("Getting test case by key {Key}", testCaseKey);

        var response = await GetAsync($"/rest/atm/1.0/testcase/{testCaseKey}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get test case by key {Key}. Status code: {StatusCode}. Response: {Response}",
                testCaseKey, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new ApiException($"Failed to get test case by key {testCaseKey}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var testCase = JsonSerializer.Deserialize<ZephyrTestCase>(content)!;

        _detailedLogService.LogDebug("Got test case {@TestCase}", testCase);

        return testCase;
    }


    public async Task<TestCaseTracesResponseWrapper?> GetTestCaseTracesV2(string testCaseKey, bool isArchived)
    {
        _logger.LogInformation("Getting test case by key {Key} (TracesV2)", testCaseKey);

        var response = await _httpClient.GetAsync(
            FromBase($"/rest/tests/1.0/testcase/search?maxResults={1}&query=testCase.key = \"{testCaseKey}\"&archived={isArchived}"));
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get test case by key {Key}. Status code: {StatusCode}. Response: {Response}",
                testCaseKey, response.StatusCode, await response.Content.ReadAsStringAsync());
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        TestCaseTracesResponseWrapper? traceRoot  = null;
        try
        {
            traceRoot = JsonSerializer.Deserialize<TestCaseTracesResponseWrapper>(content);
        }
        catch (Exception)
        {
            return null;
        }
        return traceRoot;
    }

    public async Task<TraceLinksRoot?> GetTestCaseTraces(string testCaseKey)
    {

        _logger.LogInformation("Getting test case by key {Key}", testCaseKey);
        var response = await GetAsync($"/rest/tests/1.0/testcase/{testCaseKey}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get test case by key {Key}. Status code: {StatusCode}. Response: {Response}",
                testCaseKey, response.StatusCode, await response.Content.ReadAsStringAsync());
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        var traceRoot = JsonSerializer.Deserialize<TraceLinksRoot>(content);

        return traceRoot;
    }

    public async Task<ZephyrArchivedTestCase> GetArchivedTestCase(string testCaseKey)
    {
        _logger.LogInformation("Getting test case by key {Key}", testCaseKey);

        var response = await GetAsync(
            $"/rest/tests/1.0/testcase/{testCaseKey}?fields=key,name,testScript(id,text,steps(index,reflectRef,description,text,expectedResult,testData,attachments,customFieldValues,id,stepParameters(id,testCaseParameterId,value),testCase(id,key,name,archived,majorVersion,latestVersion,parameters(id,name,defaultValue,index)))),testData,parameters(id,name,defaultValue,index),paramType");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get test case by key {Key}. Status code: {StatusCode}. Response: {Response}",
                testCaseKey, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new ApiException($"Failed to get test case by key {testCaseKey}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var testCase = JsonSerializer.Deserialize<ZephyrArchivedTestCase>(content)!;

        _detailedLogService.LogDebug("Got test case {@TestCase}", testCase);

        return testCase;
    }

    public async Task<ParametersData> GetParametersByTestCaseKey(string testCaseKey)
    {
        _logger.LogInformation("Getting parameters by test case key {Key}", testCaseKey);

        var response = await GetAsync(
            $"/rest/tests/1.0/testcase/{testCaseKey}?fields=testData,parameters(id,name,defaultValue,index),paramType");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get parameters by test case key {Key}. Status code: {StatusCode}. Response: {Response}",
                testCaseKey, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new ApiException($"Failed to get parameters by test case key {testCaseKey}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var zephyrParametersData = JsonSerializer.Deserialize<ZephyrParametersData>(content)!;
        var testData = new List<Dictionary<string, ZephyrDataParameter>>();

        if (zephyrParametersData.TestData != null && zephyrParametersData.TestData.Count != 0)
        {
            foreach (var zephyrIteration in zephyrParametersData.TestData)
            {
                var dataParameters = new Dictionary<string, ZephyrDataParameter>();

                foreach (var parameterName in zephyrIteration.Keys)
                {
                    try
                    {
                        var parameter = JsonSerializer.Deserialize<ZephyrDataParameter>(
                            zephyrIteration[parameterName].ToString()!)!;
                        dataParameters[parameterName] = parameter;
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }

                if (dataParameters.Keys.Count != 0)
                {
                    testData.Add(dataParameters);
                }
            }
        }

        var parametersData = new ParametersData
        {
            Type = zephyrParametersData.Type,
            TestData = testData,
            Parameters = (zephyrParametersData.Parameters ?? new())
                .Where(p => p.Value != null).ToList()
        };

        _detailedLogService.LogDebug("Got parameters: {@Parameters}", parametersData);

        return parametersData;
    }

    public async Task<List<JiraComponent>> GetComponents()
    {
        _logger.LogInformation("Getting components by project key {Key}", _projectKey);

        var response = await GetAsync($"/rest/api/2/project/{_projectKey}/components");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get components by project key {Key}. Status code: {StatusCode}. Response: {Response}",
                _projectKey, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new ApiException(
                $"Failed to get components by project key {_projectKey}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var components = JsonSerializer.Deserialize<List<JiraComponent>>(content);

        _detailedLogService.LogDebug("Got components: {@Issue}", components);

        return components!;
    }

    public async Task<JiraIssue> GetIssueById(string issueId)
    {
        _logger.LogInformation("Getting issue by id {IssueId}", issueId);

        var response = await GetAsync($"/rest/api/2/issue/{issueId}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get issue by id {IssueId}. Status code: {StatusCode}. Response: {Response}",
                issueId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new ApiException($"Failed to get issue by id {issueId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();

        var issue = JsonSerializer.Deserialize<JiraIssue>(content)!;

        _detailedLogService.LogDebug("Got issue: {@Issue}", issue);

        return issue;
    }

    private async Task<List<ZephyrWebLink>> GetWebLinksByTestCaseId(int testCaseId)
    {
        _logger.LogInformation("Getting web links by test case id {Id}", testCaseId);

        var response = await GetAsync(
            $"/rest/tests/1.0/testcase/{testCaseId}/tracelinks/weblink?fields=url,urlDescription");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get web links by test case id {Id}. Status code: {StatusCode}. Response: {Response}",
                testCaseId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new ApiException($"Failed to get web links by test case id {testCaseId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();

        _detailedLogService.LogDebug("Got content of web links: {Content}", content);

        var webLinks = JsonSerializer.Deserialize<List<ZephyrWebLink>>(content);

        _detailedLogService.LogDebug("Got web links: {@Issue}", webLinks);
        return webLinks!;
    }

    public async Task<List<ConfluencePageId>> GetConfluencePageIdsByTestCaseId(int testCaseId)
    {
        _logger.LogInformation("Getting confluence page ids by test case id {Id}", testCaseId);

        var response = await GetAsync(
            $"/rest/tests/1.0/testcase/{testCaseId}/tracelinks/confluencepage?fields=confluencePageId");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get confluence page ids by test case id {Id}. Status code: {StatusCode}. Response: {Response}",
                testCaseId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new ApiException(
                $"Failed to get confluence page ids by test case id {testCaseId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var confluencePageIds = JsonSerializer.Deserialize<List<ConfluencePageId>>(content);

        _detailedLogService.LogDebug("Got confluence page ids: {@Ids}", confluencePageIds);

        return confluencePageIds!;
    }

    /// <summary>
    /// Workaround for getting confluence page information for Jira test-case link.
    /// </summary>
    private async Task<List<ZephyrConfluenceLink>> GetConfluenceLinksFromConfluenceApi(string confluencePageId)
    {
        if (_unauthorizedConfluence)
        {
            _detailedLogService.LogDebug("Unauthorized Confluence API");
            throw new ApiException($"Unauthorized Confluence API");
        }
        _logger.LogInformation("Getting confluence links by confluence page id {Id} (Confluence API)", confluencePageId);

        // Deprecated jira call:
        // .GetAsync($"/rest/tests/1.0/confluence?confluencePageIds={confluencePageId}");
        var response = await GetConfluenceAsync(
            $"/rest/api/content/{confluencePageId}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get confluence links by confluence page id {Id} (Confluence API). Status code: {StatusCode}. Response: {Response}",
                confluencePageId, response.StatusCode, await response.Content.ReadAsStringAsync());
            _logger.LogWarning("These confluence links will be SKIPPED");

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _unauthorizedConfluence = true;
            }
            throw new HttpRequestException($"Failed to get confluence links by confluence page id {confluencePageId} (Confluence API). Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var jsonData = (JObject?) nsJson.JsonConvert.DeserializeObject(content);
        var title = jsonData?["title"]?.Value<string>() ?? null;
        var uri = jsonData?["_links"]?["webui"]?.Value<string>() ?? null;
        if (title != null && uri != null)
        {
            var url = _confluenceBaseUrl.ToString().TrimEnd('/') + uri;
            return [new ZephyrConfluenceLink(title, url)];
        }
        return [];
    }

    /// <summary>
    /// In fact there is always be maximum 1 value in a list
    /// </summary>
    public async Task<List<ZephyrConfluenceLink>> GetConfluenceLinksByConfluencePageId(string confluencePageId)
    {
        var confluenceLinks = await GetConfluenceLinksFromConfluenceApi(confluencePageId);

        _detailedLogService.LogDebug("Got confluence links: {@Issue}", confluenceLinks);

        // exclude broken entities
        confluenceLinks = confluenceLinks.Where(x => x.Title != null).ToList();
        return confluenceLinks;
    }

    /// <summary>
    /// Return null if there are any issue with owner parsing
    /// API: /rest/api/2/user?key={ownerKey}
    /// </summary>
    public async Task<ZephyrOwner?> GetOwner(string ownerKey)
    {
        try
        {
            _logger.LogInformation("Getting owner by key {Key}", ownerKey);
            var response = await GetAsync($"/rest/api/2/user?key={ownerKey}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get owner by key {Key}. Status code: {StatusCode}. Response: {Response}",
                    ownerKey, response.StatusCode, await response.Content.ReadAsStringAsync());
                _logger.LogWarning("This Owner (by key {Key}) will not be added as attribute", ownerKey);
                return null;
            }
            var content = await response.Content.ReadAsStringAsync();
            var owner = JsonSerializer.Deserialize<ZephyrOwner>(content);
            _detailedLogService.LogDebug("Got owner {@Owner}", owner);
            return owner;
        }
        catch (Exception e)
        {
            _logger.LogError($"Some error on ownerKey {ownerKey} parsing: {e.Message}");
        }
        return null;
    }

    public async Task<List<AltAttachmentResult>> GetAltAttachmentsForTestCase(string testCaseId)
    {
        _logger.LogInformation("Getting attachments by test case id {Id}", testCaseId);

        var response = await GetAsync(
            $"/rest/tests/1.0/testcase/{testCaseId}?fields=attachments");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get attachments by test case Id {Id}. Status code: {StatusCode}. Response: {Response}",
                testCaseId, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new ApiException(
                $"Failed to get attachments by test case key {testCaseId}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var attachmentsWrapper = JsonSerializer.Deserialize<AltAttachmentsResponse>(content);

        _detailedLogService.LogDebug("Got attachments {@Attachments}", attachmentsWrapper.Attachments);

        return attachmentsWrapper.Attachments;
    }

    public async Task<List<ZephyrAttachment>> GetAttachmentsForTestCase(string testCaseKey)
    {
        _logger.LogInformation("Getting attachments by test case key {Key}", testCaseKey);

        var response = await GetAsync(
            $"/rest/atm/1.0/testcase/{testCaseKey}/attachments");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get attachments by test case key {Key}. Status code: {StatusCode}. Response: {Response}",
                testCaseKey, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new ApiException(
                $"Failed to get attachments by test case key {testCaseKey}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var attachments = JsonSerializer.Deserialize<List<ZephyrAttachment>>(content);

        _detailedLogService.LogDebug("Got attachments {@Attachments}", attachments);

        return attachments!;
    }

    public async Task<byte[]> DownloadAttachment(string url, Guid testCaseId)
    {
        try
        {
            return await _httpClient.GetByteArrayAsync(FromBase(url));
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to download attachment {Url} for TC {Id}. Error: {Ex}", url, testCaseId.ToString(), ex);
            _logger.LogWarning("The attachment {Url} will be SKIPPED", url);
            return [];
        }
    }

    public async Task<byte[]> DownloadAttachmentById(int id, Guid testCaseId)
    {
        var url = $"/rest/tests/1.0/attachment/{id}";
        try
        {
            return await _httpClient.GetByteArrayAsync(FromBase(url));
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to download attachment (by id) {Url} for TC {Id}. Error: {Ex}", url, testCaseId.ToString(), ex);
            _logger.LogWarning("The attachment {Id} will be SKIPPED", id);
            return [];
        }
    }

    public Uri GetBaseUrl()
    {
        return _baseUrl;
    }

    private string FromBase(string uri)
    {
        return _baseUrl.ToString().TrimEnd('/') + '/' + uri.TrimStart('/');
    }

    private string FromConfluenceBase(string uri)
    {
        return _confluenceBaseUrl.ToString().TrimEnd('/') + '/' + uri.TrimStart('/');
    }

    private async Task<HttpResponseMessage> GetAsync(string requestUri)
    {
        return await _httpClient.GetAsync(FromBase(requestUri));
    }

    private async Task<HttpResponseMessage> GetConfluenceAsync(string requestUri)
    {
        return await _confluenceHttpClient.GetAsync(FromConfluenceBase(requestUri));
    }
}
