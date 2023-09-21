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

    public TestCaseService(ILogger<TestCaseService> logger, IClient client, IStepService stepService,
        IAttachmentService attachmentService)
    {
        _logger = logger;
        _client = client;
        _stepService = stepService;
        _attachmentService = attachmentService;
    }

    public async Task<List<TestCase>> ConvertTestCases(Guid projectId, Dictionary<int, Guid> sharedStepMap,
        Guid sectionId, Dictionary<string, Guid> attributeMap)
    {
        _logger.LogInformation("Converting test cases");

        var workItems = await _client.GetWorkItems(Constants.TestCaseType);

        _logger.LogDebug("Found {@WorkItems} test cases", workItems.Count);

        var testCases = new List<TestCase>();

        foreach (var workItem in workItems)
        {
            _logger.LogDebug("Converting test case: {Id}", workItem.Id);

            var testCase = await _client.GetWorkItemById(workItem.Id);

            var steps = await _stepService.ConvertSteps(
                GetValueOfField(testCase.Fields, "Microsoft.VSTS.TCM.Steps"),
                sharedStepMap
            );

            _logger.LogDebug("Found {@Steps} steps", steps.Count);

            var tmsTestCase = new TestCase
            {
                Id = Guid.NewGuid(),
                Description = GetValueOfField(testCase.Fields, "System.Description"),
                State = StateType.Ready,
                Priority = ConvertPriority(testCase.Fields["Microsoft.VSTS.Common.Priority"] as int? ?? 3),
                Steps = steps,
                PreconditionSteps = new List<Step>(),
                PostconditionSteps = new List<Step>(),
                Duration = 10,
                Attributes = new List<CaseAttribute>
                {
                    new()
                    {
                        Id = attributeMap[Constants.IterationAttributeName],
                        Value = GetValueOfField(testCase.Fields, "System.IterationPath")
                    }
                },
                Tags = new List<string>(),
                Attachments = new List<string>(),
                Iterations = new List<Iteration>(),
                Links = new List<Link>(),
                Name = GetValueOfField(testCase.Fields, "System.Title"),
                SectionId = sectionId
            };

            _logger.LogDebug("Converted test case: {@TestCase}", tmsTestCase);

            testCases.Add(tmsTestCase);
        }

        _logger.LogInformation("Exported test cases");

        return testCases;
    }

    private PriorityType ConvertPriority(int priority)
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
                _logger.LogError("Failed to convert priority {Priority}", priority);

                throw new Exception($"Failed to convert priority {priority}");
        }
    }

    private static string GetValueOfField(IDictionary<string, object> fields, string key)
    {
        if (fields.TryGetValue(key, out var value))
        {
            return value as string ?? string.Empty;
        }

        return string.Empty;
    }
}
