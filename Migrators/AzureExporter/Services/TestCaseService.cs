using AzureExporter.Client;
using AzureExporter.Models;
using Microsoft.Extensions.Logging;
using System.Text;
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

    public async Task Export()
    {
        _logger.LogInformation("Export test cases");

        var testPlans = await _client.GetTestPlans();

        var azureTestCases = new List<AzureTestCase>();

        foreach (var testPlan in testPlans.Value)
        {
            var suites = await _client.GetTestSuitesByTestPlanId(testPlan.Id);

            foreach (var suite in suites.Value)
            {
                azureTestCases.AddRange((await _client.GetTestCasesByTestPlanIdTestSuiteId(testPlan.Id, suite.Id)).Value);
            }
        }

        var testCases = new List<TestCase>();

        foreach (var azureTestCase in azureTestCases)
        {
            testCases.Add(await this.ConvertTestCase(azureTestCase));
        }

        _logger.LogInformation("Exported test cases");
    }

    protected async Task<TestCase> ConvertTestCase(AzureTestCase azureTestCase)
    {
        _logger.LogDebug("Found test case: {@AzureTestCase}", azureTestCase);

        _logger.LogDebug("Found links: {@Links}", azureTestCase.Links);

        var testCaseGuid = Guid.NewGuid();
        var steps = _stepService.ConvertSteps(azureTestCase.Fields["Microsoft.VSTS.TCM.Steps"]);
        var attachments = await _attachmentService.DownloadAttachments(azureTestCase.Fields["Microsoft.VSTS.TCM.Attachments"]);

        var testCase = new TestCase
        {
            Id = testCaseGuid,
            Name = azureTestCase.Name,
            Description = azureTestCase.Fields["Microsoft.VSTS.TCM.Decription"],
            Priority = ConvertPriority(azureTestCase.Fields["Microsoft.VSTS.Common.Priority"]),
            Tags = azureTestCase.Fields["Microsoft.VSTS.Common.Tags"],
            PreconditionSteps = new List<Step>(),
            PostconditionSteps = new List<Step>(),
            Links = azureTestCase.Links.Select(l => new Link
            {
                Url = l.Link,
            }).ToList(),
            Attributes = new List<CaseAttribute>
            {
                new()
                {
                    Value = azureTestCase.Fields["System.State"]
                }
            },
            Steps = steps,
            Attachments = attachments
        };

        _logger.LogDebug("Converted test case: {@TestCase}", testCase);

        return testCase;
    }

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
