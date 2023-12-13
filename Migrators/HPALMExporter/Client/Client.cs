using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Xml.Serialization;
using HPALMExporter.Models;
using ImportHPALMToTestIT.Models.HPALM;
using Microsoft.Extensions.Configuration;
using Serilog;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace HPALMExporter.Client;

public class Client : IClient
{
    private readonly ILogger _logger;
    private readonly string _clientId;
    private readonly string _secret;
    private readonly string _domain;
    private readonly string _projectName;
    private readonly HttpClient _httpClient;

    public Client(ILogger logger, IConfiguration configuration)
    {
        _logger = logger;

        var section = configuration.GetSection("hpalm");
        var url = section["url"];
        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException("Url is not specified");
        }

        var clientId = section["clientId"];
        if (string.IsNullOrEmpty(clientId))
        {
            throw new ArgumentException("Client ID is not specified");
        }

        var secret = section["secret"];
        if (string.IsNullOrEmpty(secret))
        {
            throw new ArgumentException("Secret is not specified");
        }

        var domainName = section["domainName"];
        if (string.IsNullOrEmpty(domainName))
        {
            throw new ArgumentException("Domain name is not specified");
        }

        var projectName = section["projectName"];
        if (string.IsNullOrEmpty(projectName))
        {
            throw new ArgumentException("Project name is not specified");
        }

        _projectName = projectName;
        _clientId = clientId;
        _secret = secret;
        _domain = domainName;

        var cookie = new CookieContainer();
        var handler = new HttpClientHandler();
        handler.CookieContainer = cookie;
        _httpClient = new HttpClient(handler);
        _httpClient.BaseAddress = new Uri(url);
    }

    public string GetProjectName()
    {
        return _projectName;
    }

    public async Task Auth()
    {
        _logger.Information("Authorizing in HP ALM");

        var dto = new AuthDto { ClientId = _clientId, Secret = _secret };

        _logger.Debug("Auth DTO: {@Dto}", dto);

        var content =
            new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, mediaType: MediaTypeNames.Application.Json);

        _logger.Debug("Connect to {Url}qcbin/rest/oauth2/login", _httpClient.BaseAddress.AbsoluteUri);

        var response = await _httpClient.PostAsync("/qcbin/rest/oauth2/login", content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            _logger.Information("Login to HP QLM success");
            return;
        }

        _logger.Error("Connect to HP ALM failed: {error}", responseString);
        throw new Exception(response.ReasonPhrase);
    }


    public async Task<List<HPALMFolder>> GetTestFolders(int id)
    {
        _logger.Information("Get test folders from HP ALM with parent id {id}", id);
        _logger.Debug(
            "Connect to {Url}qcbin/rest/domains/{domain}/projects/{project}/test-folders?query={{parent-id[={id}]}}&page-size=max",
            _httpClient.BaseAddress.AbsoluteUri,
            _domain,
            _projectName,
            id);

        var response =
            await _httpClient.GetStringAsync(
                $"/qcbin/rest/domains/{_domain}/projects/{_projectName}/test-folders?query={{parent-id[={id}]}}&page-size=max");

        _logger.Debug("Response string: {response}", response);

        var serializer = new XmlSerializer(typeof(Entities));
        Entities entities;
        using (var sr = new StringReader(response))
        {
            entities = (Entities)serializer.Deserialize(sr);
        }

        var folders = entities.Entity.Select(e => e.ToTestFolder()).ToList();

        while (folders.Count < entities.TotalResults)
        {
            var startIndex = folders.Count + 1;
            response =
                await _httpClient.GetStringAsync(
                    $"/qcbin/rest/domains/{_domain}/projects/{_projectName}/test-folders?query={{parent-id[={id}]}}&page-size=max&start-index={startIndex}");

            _logger.Debug("Response string: {response}", response);

            serializer = new XmlSerializer(typeof(Entities));
            using (var sr = new StringReader(response))
            {
                entities = (Entities)serializer.Deserialize(sr);
            }

            folders.AddRange(entities.Entity.Select(e => e.ToTestFolder()).ToList());
        }

        _logger.Information("Found {count} folders", folders.Count);
        _logger.Debug("Folders: {@folders}", folders);

        return folders;
    }

    public async Task<List<HPALMTest>> GetTests(int folderId, IEnumerable<string> attributes)
    {
        _logger.Information("Get tests from HP ALM from folder {id}", folderId);
        _logger.Debug(
            "Connect to {Url}qcbin/rest/domains/{domain}/projects/{project}/tests?query={{parent-id[={folderId}]}}&page-size=max",
            _httpClient.BaseAddress.AbsoluteUri,
            _domain,
            _projectName,
            folderId);

        var response =
            await _httpClient.GetStringAsync(
                $"/qcbin/rest/domains/{_domain}/projects/{_projectName}/tests?query={{parent-id[={folderId}]}}&page-size=max");

        _logger.Debug("Response string: {response}", response);

        var serializer = new XmlSerializer(typeof(Entities));
        Entities entities;
        using (var sr = new StringReader(response))
        {
            entities = (Entities)serializer.Deserialize(sr);
        }

        var tests = entities.Entity.Select(e => e.ToTest(attributes)).ToList();


        while (tests.Count < entities.TotalResults)
        {
            var startIndex = tests.Count + 1;
            response =
                await _httpClient.GetStringAsync(
                    $"/qcbin/rest/domains/{_domain}/projects/{_projectName}/tests?query={{parent-id[={folderId}]}}&page-size=max&start-index={startIndex}");

            _logger.Debug("Response string: {response}", response);

            serializer = new XmlSerializer(typeof(Entities));
            using (var sr = new StringReader(response))
            {
                entities = (Entities)serializer.Deserialize(sr);
            }

            tests.AddRange(entities.Entity.Select(e => e.ToTest(attributes)).ToList());
        }

        _logger.Information("Found {count} tests", tests.Count);
        _logger.Debug("Tests: {@tests}", tests);

        return tests;
    }

    public async Task<HPALMTest> GetTest(int testId, IEnumerable<string> attributes)
    {
        _logger.Information("Get test {Id} from HP ALM", testId);
        _logger.Debug(
            "Connect to {Url}qcbin/rest/domains/{domain}/projects/{project}/tests/{testId}",
            _httpClient.BaseAddress.AbsoluteUri,
            _domain,
            _projectName,
            testId);

        try
        {
            var response =
                await _httpClient.GetStringAsync(
                    $"/qcbin/rest/domains/{_domain}/projects/{_projectName}/tests/{testId}");

            _logger.Debug("Response string: {response}", response);

            var serializer = new XmlSerializer(typeof(Entity));
            Entity entity;
            using (var sr = new StringReader(response))
            {
                entity = (Entity)serializer.Deserialize(sr);
            }

            var test = entity.ToTest(attributes);

            _logger.Debug("Test: {@test}", test);

            return test;
        }
        catch (Exception e)
        {
            _logger.Debug("Can not get test with ID {id}", testId);
            return null;
        }
    }

    public async Task<List<HPALMStep>> GetSteps(int testId)
    {
        _logger.Information("Get steps from HP ALM from test {id}", testId);
        _logger.Debug(
            "Connect to {Url}qcbin/rest/domains/{domain}/projects/{project}/design-steps?query={{parent-id[={testId}]}}",
            _httpClient.BaseAddress.AbsoluteUri,
            _domain,
            _projectName,
            testId);

        var response =
            await _httpClient.GetStringAsync(
                $"/qcbin/rest/domains/{_domain}/projects/{_projectName}/design-steps?query={{parent-id[={testId}]}}");

        _logger.Debug("Response string: {response}", response);

        var serializer = new XmlSerializer(typeof(Entities));
        Entities entities;
        using (var sr = new StringReader(response))
        {
            entities = (Entities)serializer.Deserialize(sr);
        }

        var steps = entities.Entity.Select(e => e.ToStep()).ToList();

        _logger.Information("Found {count} steps", steps.Count);
        _logger.Debug("Steps: {@steps}", steps);

        return steps;
    }

    public async Task<List<HPALMAttachment>> GetAttachmentsFromTest(int testId)
    {
        _logger.Information("Get test attachments from HP ALM from test {id}", testId);
        _logger.Debug(
            "Connect to {Url}qcbin/rest/domains/{domain}/projects/{project}/tests/{testId}/attachments",
            _httpClient.BaseAddress.AbsoluteUri,
            _domain,
            _projectName,
            testId);

        var response =
            await _httpClient.GetStringAsync(
                $"/qcbin/rest/domains/{_domain}/projects/{_projectName}/tests/{testId}/attachments");

        _logger.Debug("Response string: {response}", response);

        var serializer = new XmlSerializer(typeof(Entities));
        Entities entities;
        using (var sr = new StringReader(response))
        {
            entities = (Entities)serializer.Deserialize(sr);
        }

        var attachments = entities.Entity.Select(e => e.ToAttachment()).ToList();

        _logger.Information("Found {count} attachments", attachments.Count);
        _logger.Debug("Attachments: {@attachments}", attachments);

        return attachments;
    }

    public async Task<List<HPALMAttachment>> GetAttachmentsFromStep(int stepId)
    {
        _logger.Information("Get step attachments from HP ALM from step {id}", stepId);
        _logger.Debug(
            "Connect to {Url}qcbin/rest/domains/{domain}/projects/{project}/design-steps/{stepId}/attachments",
            _httpClient.BaseAddress.AbsoluteUri,
            _domain,
            _projectName,
            stepId);

        var response =
            await _httpClient.GetStringAsync(
                $"/qcbin/rest/domains/{_domain}/projects/{_projectName}/design-steps/{stepId}/attachments");

        _logger.Debug("Response string: {response}", response);

        var serializer = new XmlSerializer(typeof(Entities));
        Entities entities;
        using (var sr = new StringReader(response))
        {
            entities = (Entities)serializer.Deserialize(sr);
        }

        var attachments = entities.Entity.Select(e => e.ToAttachment()).ToList();

        _logger.Information("Found {count} attachments", attachments.Count);
        _logger.Debug("Attachments: {@attachments}", attachments);

        return attachments;
    }

    public async Task<byte[]> DownloadAttachment(int testId, string attachName)
    {
        _logger.Information("Download attachment from HP ALM with test id {testId} and name {attachName}", testId,
            attachName);
        _logger.Debug(
            "Connect to {Url}qcbin/rest/domains/{domain}/projects/{project}/tests/{testId}/attachments/{attachName}",
            _httpClient.BaseAddress.AbsoluteUri,
            _domain,
            _projectName,
            testId,
            attachName);

        using var requestMessage =
            new HttpRequestMessage(HttpMethod.Get,
                $"{_httpClient.BaseAddress.AbsoluteUri}qcbin/rest/domains/{_domain}/projects/{_projectName}/tests/{testId}/attachments/{attachName}");
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));

        _logger.Debug("Request: {@Request}", requestMessage);

        var httpResult =
            await _httpClient.SendAsync(requestMessage);
       var content = await httpResult.Content.ReadAsByteArrayAsync();

       return content;
    }

    public async Task<List<HPALMParameter>> GetParameters(int testId)
    {
        _logger.Information("Get parameters from HP ALM from test {id}", testId);
        _logger.Debug(
            "Connect to {Url}qcbin/rest/domains/{domain}/projects/{project}/test-parameters?query={{parent-id[={testId}]}}",
            _httpClient.BaseAddress.AbsoluteUri,
            _domain,
            _projectName,
            testId);

        var response =
            await _httpClient.GetStringAsync(
                $"/qcbin/rest/domains/{_domain}/projects/{_projectName}/test-parameters?query={{parent-id[={testId}]}}");

        _logger.Debug("Response string: {response}", response);

        var serializer = new XmlSerializer(typeof(Entities));
        Entities entities;
        using (var sr = new StringReader(response))
        {
            entities = (Entities)serializer.Deserialize(sr);
        }

        var parameters = entities.Entity.Select(e => e.ToParameter()).ToList();

        _logger.Information("Found {count} parameters", parameters.Count);
        _logger.Debug("Parameters: {@parameters}", parameters);

        return parameters;
    }

    public async Task<HPALMAttributes> GetTestAttributes()
    {
        _logger.Information("Get test custom attributes from HP ALM");
        _logger.Debug(
            "Connect to {Url}qcbin/rest/domains/{domain}/projects/{project}/customization/entities/test/fields?alt=application/json",
            _httpClient.BaseAddress.AbsoluteUri,
            _domain,
            _projectName);

        var response = await _httpClient.GetAsync(
            $"/qcbin/rest/domains/{_domain}/projects/{_projectName}/customization/entities/test/fields?alt=application/json");
        var responseString = await response.Content.ReadAsStringAsync();

        _logger.Debug("Response string: {response}", responseString);

        if (response.IsSuccessStatusCode)
            return JsonSerializer.Deserialize<HPALMAttributes>(responseString);

        _logger.Error("Get test custom attributes failed");

        throw new Exception(response.ReasonPhrase);
    }

    public async Task<HPALMLists> GetLists()
    {
        _logger.Information("Get list attributes from HP ALM");
        _logger.Debug(
            "Connect to {Url}qcbin/rest/domains/{domain}/projects/{project}/customization/entities/test/lists?alt=application/json",
            _httpClient.BaseAddress.AbsoluteUri,
            _domain,
            _projectName);

        var response = await _httpClient.GetAsync(
            $"/qcbin/rest/domains/{_domain}/projects/{_projectName}/customization/entities/test/lists?alt=application/json");

        var responseString = await response.Content.ReadAsStringAsync();

        _logger.Debug("Response string: {response}", responseString);

        if (response.IsSuccessStatusCode)
            return JsonSerializer.Deserialize<HPALMLists>(responseString);

        _logger.Error("Get list attributes failed");

        throw new Exception(response.ReasonPhrase);
    }
}
