using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZephyrScaleServerExporter.Client.Exceptions;
using ZephyrScaleServerExporter.Models.TestCases;
using ZephyrScaleServerExporter.Services;

namespace ZephyrScaleServerExporter.Client;

public class TestCaseClient(
    IDetailedLogService detailedLogService,
    ILogger<TestCaseClient> logger)
    : ITestCaseClient
{
    
    public async Task<List<ZephyrTestCase>> GetTestCasesCoreHandlerNewApi(HttpClient httpClient, string projectKey, string reqString)
    {
        detailedLogService.LogDebug("reqString: {ReqString}", reqString);
        var response = await httpClient.GetAsync(reqString);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Failed to get test cases by project key {Key}. Status code: {StatusCode}. Response: {Response}",
                projectKey, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new ApiException($"Failed to get test cases by project key {projectKey}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var tcRoots = DeserializeTestCasesNewApi(content);
        detailedLogService.LogDebug("content: {ReqString}", content);
        return tcRoots.ToTestCases();
    }
    
    public async Task<List<ZephyrTestCase>> GetTestCasesCoreHandler(HttpClient httpClient, string projectKey, string reqString)
    {
        detailedLogService.LogDebug("reqString: {ReqString}", reqString);
        var response = await httpClient.GetAsync(reqString);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Failed to get test cases by project key {Key}. Status code: {StatusCode}. Response: {Response}",
                projectKey, response.StatusCode, await response.Content.ReadAsStringAsync());

            throw new ApiException($"Failed to get test cases by project key {projectKey}. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        return DeserializeTestCases(content);
    }

    private List<ZephyrTestCaseRoot> DeserializeTestCasesNewApi(string content)
    {
        var resultsResponse = JsonSerializer.Deserialize<TestCaseResponseWrapper>(content)!;

        var testCases = resultsResponse.Results;
        testCases.ForEach(tc =>
        {
            if (!string.IsNullOrEmpty(tc.Name))
            {
                tc.Name = ReplaceNewlinesWithSpaces(tc.Name);
            }

            if (tc.TestScript?.Steps == null) {
                return;
            }
            tc.TestScript.Steps.ForEach(step =>
            {
                if (step.Attachments == null) {
                    step.Attachments = [];
                    return;
                }

                step.Attachments = step.Attachments
                    .Where(attach => attach is
                    {
                        Name: not null, FileName: not null, CreatedOn: not null, UserKey: not null
                    }).ToList();
                
            });
        });
        
        detailedLogService.LogDebug("Got test cases {@TestCases}", testCases);

        return testCases;
    }
    private List<ZephyrTestCase> DeserializeTestCases(string content)
    {
        var testCases = JsonSerializer.Deserialize<List<ZephyrTestCase>>(content)!;

        testCases.ForEach(tc =>
        {
            if (!string.IsNullOrEmpty(tc.Name))
            {
                tc.Name = ReplaceNewlinesWithSpaces(tc.Name);
            }

            if (tc.TestScript?.Steps == null) {
                return;
            }
            tc.TestScript.Steps.ForEach(step =>
            {
                if (step.Attachments == null) {
                    step.Attachments = [];
                    return;
                }

                step.Attachments = step.Attachments
                    .Where(attach => attach is
                    {
                        Name: not null, FileName: not null, CreatedOn: not null, UserKey: not null
                    }).ToList();
                
            });
        });
        
        detailedLogService.LogDebug("Got test cases {@TestCases}", testCases);

        return testCases;
    }

    private static string ReplaceNewlinesWithSpaces(string inputText)
    {
        if (string.IsNullOrEmpty(inputText))
        {
            return inputText;
        }
        // Replace common newline sequences with a single space.
        // Order is important to handle \r\n correctly.
        string result = inputText.Replace("\r\n", " ")
                                 .Replace("\n", " ")
                                 .Replace("\r", " ");
        return result;
    }

}