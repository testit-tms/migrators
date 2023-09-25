using AzureExporter.Client;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Models;
using Constants = AzureExporter.Models.Constants;
using Link = Models.Link;

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

        var workItemsIds = await _client.GetWorkItemIds(Constants.TestCaseType);

        _logger.LogDebug("Found {@WorkItems} test cases", workItemsIds.Count);

        var testCases = new List<TestCase>();

        foreach (var workItemId in workItemsIds)
        {
            _logger.LogDebug("Converting test case: {Id}", workItemId);

            var testCase = await _client.GetWorkItemById(workItemId);
            var steps = _stepService.ConvertSteps(testCase.Steps, sharedStepMap);

            _logger.LogDebug("Found {@Steps} steps", steps.Count);

            var testCaseGuid = Guid.NewGuid();
            var tmsAttachments = await _attachmentService.DownloadAttachments(testCase.Attachments, testCaseGuid);

            var tmsTestCase = new TestCase
            {
                Id = testCaseGuid,
                Description = testCase.Description,
                State = StateType.Ready,
                Priority = ConvertPriority(testCase.Priority),
                Steps = steps,
                PreconditionSteps = new List<Step>(),
                PostconditionSteps = new List<Step>(),
                Duration = 10,
                Attributes = new List<CaseAttribute>
                {
                    new()
                    {
                        Id = attributeMap[Constants.IterationAttributeName],
                        Value = testCase.IterationPath
                    },
                    new()
                    {
                        Id = attributeMap[Constants.StateAttributeName],
                        Value = testCase.State
                    }
                },
                Tags = ConvertTags(testCase.Tags),
                Attachments = tmsAttachments,
                Iterations = new List<Iteration>(),
                Links = new List<Link>(), //ConvertLinks(testCase.Relations.ToList()),
                Name = testCase.Title,
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

    private List<Link> ConvertLinks(List<WorkItemRelation> relations)
    {
        var links = new List<Link>();

        foreach (var relation in relations)
        {
            if (relation.Rel.Equals("ArtifactLink"))
            {
                links.Add(
                    new Link
                    {
                        Url = relation.Url,
                        Description = relation.Attributes["name"] as string
                    }
                );
            }
        }

        return links;
    }

    private List<string> ConvertTags(string tagsContent)
    {
        return string.IsNullOrEmpty(tagsContent) ? new List<string>() : tagsContent.Split("; ").ToList();
    }
}
