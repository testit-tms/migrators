using AzureExporter.Client;
using Microsoft.Extensions.Logging;
using Models;
using Constants = AzureExporter.Models.Constants;

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

    public async Task<List<TestCase>> ConvertTestCases(Guid projectId, Dictionary<int, Guid> sharedStepMap, Guid sectionId, Dictionary<string, Guid> attributeMap)
    {
        _logger.LogInformation("Converting test cases");

        var workItems = await _client.GetWorkItems(Constants.TestCaseType);

        _logger.LogDebug("Found {@WorkItems} test cases", workItems.Count);

        var testCases = new List<TestCase>();

        foreach (var workItem in workItems)
        {
            _logger.LogDebug("Converting test case: {Id}", workItem.Id);

            var testCase = await _client.GetWorkItemById(workItem.Id);

            var steps = new List<Step>();

            if (testCase.Fields.Keys.Contains("Microsoft.VSTS.TCM.Steps"))
            {
                steps = await _stepService.ConvertSteps(testCase.Fields["Microsoft.VSTS.TCM.Steps"] as string,
                    sharedStepMap);
            }

            _logger.LogDebug("Found {@Steps} steps", steps.Count);

            var tmsTestCase = new TestCase()
            {
                Id = Guid.NewGuid(),
                Description = "",
                State = StateType.Ready,
                Priority = PriorityType.Medium,
                Steps = steps,
                PreconditionSteps = new List<Step>(),
                PostconditionSteps = new List<Step>(),
                Duration = 10,
                Attributes = new List<CaseAttribute>(),
                Tags = new List<string>(),
                Attachments = new List<string>(),
                Iterations = new List<Iteration>(),
                Links = new List<Link>(),
                Name = testCase.Fields["System.Title"] as string,
                SectionId = sectionId
            };

            _logger.LogDebug("Converted test case: {@TestCase}", tmsTestCase);

            testCases.Add(tmsTestCase);
        }

        _logger.LogInformation("Exported test cases");

        return testCases;
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
