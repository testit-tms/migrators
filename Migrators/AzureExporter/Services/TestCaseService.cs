using AzureExporter.Client;
using AzureExporter.Models;
using Microsoft.Extensions.Logging;
using Models;

namespace AzureExporter.Services;

public class TestCaseService : ITestCaseService
{
    private readonly ILogger<TestCaseService> _logger;
    private readonly IClient _client;
    private readonly IStepService _stepService;
    private readonly IAttachmentService _attachmentService;

    public TestCaseService(ILogger<TestCaseService> logger, IClient client, IStepService stepService, IAttachmentService attachmentService)
    {
        _logger = logger;
        _client = client;
        _stepService = stepService;
        _attachmentService = attachmentService;
    }

    public async Task<List<TestCase>> Export()
    {
        _logger.LogInformation("Export test cases");

        var projectId = await _client.GetProjectId();
        var testPlans = await _client.GetTestPlansByProjectId(projectId);

        var workItems = new List<Microsoft.VisualStudio.Services.TestManagement.TestPlanning.WebApi.TestCase>();

        foreach (var testPlan in testPlans)
        {
            var testSuites = await _client.GetTestSuitesByProjectIdAndTestPlanId(projectId, testPlan.Id);

            foreach (var testSuite in testSuites)
            {
                var testCases = await _client.GetTestCaseListByProjectIdAndTestPlanIdAndSuiteId(projectId, testPlan.Id, testSuite.Id);

                workItems.AddRange(testCases);
            }
        }

        //var testPlans = await _client.GetTestPlans();

        //var azureTestPoints = new List<AzureTestPoint>();

        //foreach (var testPlan in testPlans.Value)
        //{
        //    var suites = (await _client.GetTestSuitesByTestPlanId(testPlan.Id)).Value;

        //    //TODO: take out the suite cycle after adding the Plan property to AzureSuite model
        //    foreach (var suite in suites)
        //    {
        //        azureTestPoints.AddRange((await _client.GetTestCasesByTestPlanIdTestSuiteId(testPlan.Id, suite.Id)).Value);
        //    }
        //}

        ////var azureTestSuites = new List<AzureSuite>();

        ////foreach (var suite in azureTestSuites)
        ////{
        ////    azureTestPoints.AddRange((await _client.GetTestCasesByTestPlanIdTestSuiteId(suite.Plan.Id, suite.Id)).Value);
        ////}

        //var azureWorkItems = new List<AzureWorkItem>();

        //foreach (var azureTestPoint in azureTestPoints)
        //{
        //    azureWorkItems.Add(await _client.GetWorkItemById(azureTestPoint.TestCase.Id));
        //}

        //var testCases = new List<TestCase>();

        //foreach (var azureWorkItem in azureWorkItems)
        //{
        //    testCases.Add(await this.ConvertTestCase(azureWorkItem));
        //}

        _logger.LogInformation("Exported test cases");

        return new List<TestCase>();
    }

    //protected async Task<TestCase> ConvertTestCase(AzureWorkItem azureTestCase)
    //{
    //    _logger.LogDebug("Found test case: {@AzureTestCase}", azureTestCase);

    //    var testCaseGuid = Guid.NewGuid();
    //    var steps = _stepService.ConvertSteps(azureTestCase.Fields["Microsoft.VSTS.TCM.Steps"]);
    //    var attachments = await _attachmentService.DownloadAttachments(azureTestCase.Fields["Microsoft.VSTS.TCM.Attachments"]);

    //    var testCase = new TestCase
    //    {
    //        Id = testCaseGuid,
    //        Name = azureTestCase.Fields["System.Name"],
    //        Description = azureTestCase.Fields["System.Decription"],
    //        Priority = ConvertPriority(azureTestCase.Fields["Microsoft.VSTS.Common.Priority"]),
    //        Tags = azureTestCase.Fields["Microsoft.VSTS.Common.Tags"],
    //        PreconditionSteps = new List<Step>(),
    //        PostconditionSteps = new List<Step>(),
    //        Links = azureTestCase.Links.Select(l => new Link
    //        {
    //            Url = l.Link,
    //        }).ToList(),
    //        Attributes = new List<CaseAttribute>
    //        {
    //            new()
    //            {
    //                Value = azureTestCase.Fields["System.State"]
    //            }
    //        },
    //        Steps = steps,
    //        Attachments = attachments
    //    };

    //    _logger.LogDebug("Converted test case: {@TestCase}", testCase);

    //    return testCase;
    //}

    protected PriorityType ConvertPriority(int priority)
    {
        switch (priority)
        {
            case 1:
                return PriorityType.Highest;
            case 2:
                return PriorityType.High;
            case 3:
                return PriorityType.Medium;
            case 4:
                return PriorityType.Low;
            default:
                _logger.LogError($"Failed to convert priority {priority}");

                throw new Exception($"Failed to convert priority {priority}");
        }
    }
}
