using AzureExporter.Client;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Models;
using Constants = AzureExporter.Models.Constants;
using Link = Models.Link;

namespace AzureExporter.Services;

public class TestCaseService : WorkItemBaseService, ITestCaseService
{
    private readonly ILogger<TestCaseService> _logger;
    private readonly IClient _client;
    private readonly IStepService _stepService;
    private readonly IAttachmentService _attachmentService;
    private readonly ILinkService _linkService;

    public TestCaseService(ILogger<TestCaseService> logger, IClient client, IStepService stepService,
        IAttachmentService attachmentService, ILinkService linkService)
    {
        _logger = logger;
        _client = client;
        _stepService = stepService;
        _attachmentService = attachmentService;
        _linkService = linkService;
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
            var links = _linkService.CovertLinks(testCase.Links);

            var tmsTestCase = new TestCase
            {
                Id = testCaseGuid,
                Description = testCase.Description,
                State = StateType.NotReady,
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
                Links = links, //ConvertLinks(testCase.Relations.ToList()),
                Name = testCase.Title,
                SectionId = sectionId
            };

            _logger.LogDebug("Converted test case: {@TestCase}", tmsTestCase);

            testCases.Add(tmsTestCase);
        }

        _logger.LogInformation("Exported test cases");

        return testCases;
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
}
