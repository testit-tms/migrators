//using AzureExporter.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using AzureExporter.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace AzureExporter.Client;

public class Client : IClient
{
    private readonly ILogger<Client> __logger;
    private readonly HttpClient __httpClient;
    private readonly string __projectName;
    private readonly string __organisationName;

    public Client(ILogger<Client> logger, IConfiguration configuration)
    {
        this.__logger = logger;

        var section = configuration.GetSection("azure");
        var url = section["url"];

        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException("Url is not specified");
        }

        var token = section["token"];

        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentException("Private token is not specified");
        }

        var projectName = section["projectName"];

        if (string.IsNullOrEmpty(projectName))
        {
            throw new ArgumentException("Project name is not specified");
        }

        var organisationName = section["organisationName"];
        if (string.IsNullOrEmpty(organisationName))
        {
            throw new ArgumentException("Organisation name is not specified");
        }

        this.__projectName = projectName;
        this.__organisationName = organisationName;

        __httpClient = new HttpClient();
        __httpClient.BaseAddress = new Uri(url);
        __httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Api-Token", token);
    }

    public async Task<Wiql> GetWorkItems()
    {
        Dictionary<string, string> body = new Dictionary<string, string>
        {
            {
                "query",
                "Select [System.Id] From WorkItems order by [Microsoft.VSTS.Common.Priority] asc, [System.CreatedDate] desc"
            }
        };
        var stringContent = new StringContent(body.ToString());

        var response = await __httpClient.PostAsync($"{__organisationName}/{__projectName}/_apis/wit/wiql?api-version=7.0", stringContent);

        if (!response.IsSuccessStatusCode)
        {
            __logger.LogError($"Failed to get work items. Status code: {response.StatusCode}. Response: {await response.Content.ReadAsStringAsync()}");

            throw new Exception($"Failed to get work items. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<Wiql>(content);
    }

    public async Task<TestCase> GetWorkItemById(int id)
    {
        var response = await __httpClient.GetAsync($"{__organisationName}/{__projectName}/_apis/wit/workitems/{id}?api-version=7.0");

        if (!response.IsSuccessStatusCode)
        {
            __logger.LogError($"Failed to get work item by id {id}. Status code: {response.StatusCode}. Response: {await response.Content.ReadAsStringAsync()}");

            throw new Exception($"Failed to get work item by id {id}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<TestCase>(content);
    }

    public async Task<string> GetAttachmentById(int id)
    {
        var response = await __httpClient.GetAsync($"{__organisationName}/{__projectName}/_apis/wit/attachments/{id}?api-version=7.0");

        if (!response.IsSuccessStatusCode)
        {
            __logger.LogError($"Failed to get attachment by id {id}. Status code: {response.StatusCode}. Response: {await response.Content.ReadAsStringAsync()}");

            throw new Exception($"Failed to get attachment by id {id}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();

        return content;
    }
}
