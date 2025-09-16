// using System.Net.Http.Headers;
// using System.Text.Json;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.Logging;
//
// namespace ZephyrScaleServerExporter.Client.Cloud;
//
// public class CloudClient : IClient
// {
//     private readonly ILogger<Client> _logger;
//     private readonly HttpClient _httpClient;
//     private readonly string _projectName;
//
//     public Client(ILogger<Client> logger, IConfiguration configuration)
//     {
//         _logger = logger;
//
//         var section = configuration.GetSection("zephyr");
//         var url = section["url"];
//         if (string.IsNullOrEmpty(url))
//         {
//             throw new ArgumentException("Url is not specified");
//         }
//
//         var token = section["token"];
//         if (string.IsNullOrEmpty(token))
//         {
//             throw new ArgumentException("Token is not specified");
//         }
//
//         var projectName = section["projectName"];
//         if (string.IsNullOrEmpty(projectName))
//         {
//             throw new ArgumentException("Project name is not specified");
//         }
//
//         _projectName = projectName;
//         _httpClient = new HttpClient();
//         _httpClient.BaseAddress = new Uri(url);
//         _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
//     }
//
//     public async Task<ZephyrProject> GetProject()
//     {
//         _logger.LogInformation("Getting project {ProjectName}", _projectName);
//
//         var response = await _httpClient.GetAsync("projects");
//         if (!response.IsSuccessStatusCode)
//         {
//             _logger.LogError("Failed to get project. Status code: {StatusCode}. Response: {Response}",
//                 response.StatusCode, await response.Content.ReadAsStringAsync());
//
//             throw new Exception($"Failed to get project. Status code: {response.StatusCode}");
//         }
//
//         var content = await response.Content.ReadAsStringAsync();
//         var projects = JsonSerializer.Deserialize<ZephyrProjects>(content);
//         var project = projects?.Projects.FirstOrDefault(p =>
//             string.Equals(p.Key, _projectName, StringComparison.InvariantCultureIgnoreCase));
//
//         if (project != null) return project;
//
//         _logger.LogError("Project not found");
//
//         throw new Exception("Project not found");
//     }
//
//     public async Task<List<ZephyrStatus>> GetStatuses()
//     {
//         _logger.LogInformation("Getting statuses");
//
//         var response = await _httpClient.GetAsync($"statuses?projectKey={_projectName}&statusType=TEST_CASE");
//         if (!response.IsSuccessStatusCode)
//         {
//             _logger.LogError("Failed to get statuses. Status code: {StatusCode}. Response: {Response}",
//                 response.StatusCode, await response.Content.ReadAsStringAsync());
//
//             throw new Exception($"Failed to get statuses. Status code: {response.StatusCode}");
//         }
//
//         var content = await response.Content.ReadAsStringAsync();
//         var statuses = JsonSerializer.Deserialize<ZephyrStatuses>(content);
//
//         _logger.LogDebug("Got statuses {@Statuses}", statuses);
//
//         return statuses.Statuses;
//     }
//
//     public async Task<List<ZephyrPriority>> GetPriorities()
//     {
//         _logger.LogInformation("Getting priorities");
//
//         var response = await _httpClient.GetAsync($"priorities?projectKey={_projectName}");
//         if (!response.IsSuccessStatusCode)
//         {
//             _logger.LogError("Failed to get priorities. Status code: {StatusCode}. Response: {Response}",
//                 response.StatusCode, await response.Content.ReadAsStringAsync());
//
//             throw new Exception($"Failed to get priorities. Status code: {response.StatusCode}");
//         }
//
//         var content = await response.Content.ReadAsStringAsync();
//         var priorities = JsonSerializer.Deserialize<ZephyrPriorities>(content);
//
//         _logger.LogDebug("Got priorities {@Priorities}", priorities);
//
//         return priorities.Priorities;
//     }
//
//     public async Task<List<ZephyrFolder>> GetFolders()
//     {
//         _logger.LogInformation("Getting folders");
//
//         var response = await _httpClient.GetAsync($"folders?projectKey={_projectName}&folderType=TEST_CASE");
//         if (!response.IsSuccessStatusCode)
//         {
//             _logger.LogError("Failed to get folders. Status code: {StatusCode}. Response: {Response}",
//                 response.StatusCode, await response.Content.ReadAsStringAsync());
//
//             throw new Exception($"Failed to get folders. Status code: {response.StatusCode}");
//         }
//
//         var content = await response.Content.ReadAsStringAsync();
//         var folders = JsonSerializer.Deserialize<ZephyrFolders>(content);
//
//         _logger.LogDebug("Got folders {@Folders}", folders);
//
//         return folders.Folders;
//     }
//
//     public async Task<List<ZephyrTestCase>> GetTestCases(int folderId)
//     {
//         _logger.LogInformation("Getting test cases for folder {FolderId}", folderId);
//
//         var allTestCases = new List<ZephyrTestCase>();
//         var startAt = 0;
//         var maxResults = 100;
//         var isLast = false;
//
//         do
//         {
//             var response = await _httpClient.GetAsync($"testcases?maxResults={maxResults}&startAt={startAt}&projectKey={_projectName}&folderId={folderId}");
//             if (!response.IsSuccessStatusCode)
//             {
//                 _logger.LogError(
//                     "Failed to get test cases for folder {FolderId}. Status code: {StatusCode}. Response: {Response}",
//                     folderId, response.StatusCode, await response.Content.ReadAsStringAsync());
//
//                 throw new Exception($"Failed to get test cases for folder {folderId}. Status code: {response.StatusCode}");
//             }
//
//             var content = await response.Content.ReadAsStringAsync();
//             var testCases = JsonSerializer.Deserialize<ZephyrTestCases>(content);
//             isLast = testCases.IsLast;
//
//             allTestCases.AddRange(testCases.TestCases);
//
//             startAt += maxResults;
//
//             _logger.LogDebug("Got test cases {@TestCases}", testCases);
//         } while (!isLast);
//
//         return allTestCases;
//     }
//
//     public async Task<List<ZephyrStep>> GetSteps(string testCaseKey)
//     {
//         _logger.LogInformation("Getting steps for test case {TestCaseKey}", testCaseKey);
//
//         var response = await _httpClient.GetAsync($"testcases/{testCaseKey}/teststeps");
//         if (!response.IsSuccessStatusCode)
//         {
//             _logger.LogError(
//                 "Failed to get steps for test case {TestCaseKey}. Status code: {StatusCode}. Response: {Response}",
//                 testCaseKey, response.StatusCode, await response.Content.ReadAsStringAsync());
//
//             throw new Exception($"Failed to get steps for test case {testCaseKey}. Status code: {response.StatusCode}");
//         }
//
//         var content = await response.Content.ReadAsStringAsync();
//         var steps = JsonSerializer.Deserialize<ZephyrSteps>(content);
//
//         _logger.LogDebug("Got steps {@Steps}", steps);
//
//         return steps.Steps;
//     }
//
//     public async Task<ZephyrTestScript> GetTestScript(string testCaseKey)
//     {
//         _logger.LogInformation("Getting test script for test case {TestCaseKey}", testCaseKey);
//
//         var response = await _httpClient.GetAsync($"testcases/{testCaseKey}/testscript");
//         if (!response.IsSuccessStatusCode)
//         {
//             _logger.LogError(
//                 "Failed to get test script for test case {TestCaseKey}. Status code: {StatusCode}. Response: {Response}",
//                 testCaseKey, response.StatusCode, await response.Content.ReadAsStringAsync());
//
//             throw new Exception(
//                 $"Failed to get test script for test case {testCaseKey}. Status code: {response.StatusCode}");
//         }
//
//         var content = await response.Content.ReadAsStringAsync();
//         var testScript = JsonSerializer.Deserialize<ZephyrTestScript>(content);
//
//         _logger.LogDebug("Got test script {@TestScript}", testScript);
//
//         return testScript;
//     }
//
//     public async Task<byte[]> DownloadAttachment(string url)
//     {
//         var httpClient = new HttpClient();
//
//         return await httpClient.GetByteArrayAsync(url);
//     }
// }
