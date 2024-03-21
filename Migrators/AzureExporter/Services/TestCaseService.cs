using AzureExporter.Client;
using Microsoft.Extensions.Logging;
using Models;
using Constants = AzureExporter.Models.Constants;

namespace AzureExporter.Services;

public class TestCaseService : WorkItemBaseService, ITestCaseService
{
    private readonly ILogger<TestCaseService> _logger;
    private readonly IClient _client;
    private readonly IStepService _stepService;
    private readonly IAttachmentService _attachmentService;
    private readonly ILinkService _linkService;
    private readonly IParameterService _parameterService;
    public const int _duration = 10000;

    public TestCaseService(ILogger<TestCaseService> logger, IClient client, IStepService stepService,
        IAttachmentService attachmentService, ILinkService linkService, IParameterService parameterService)
    {
        _logger = logger;
        _client = client;
        _stepService = stepService;
        _attachmentService = attachmentService;
        _linkService = linkService;
        _parameterService = parameterService;
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

            _logger.LogDebug("Found test case with {Id}: {@TestCase}", testCase.Id, testCase);

            var steps = _stepService.ConvertSteps(testCase.Steps, sharedStepMap);

            _logger.LogDebug("Found {@Steps} steps", steps.Count);

            var testCaseGuid = Guid.NewGuid();
            var tmsAttachments = await _attachmentService.DownloadAttachments(testCase.Attachments, testCaseGuid);
            var links = _linkService.CovertLinks(testCase.Links);
            var parameters = _parameterService.ConvertParameters(testCase.Parameters);
            var iterations = parameters
                .Select(p =>
                    new Iteration
                    {
                        Parameters = p.Select(pair => new Parameter
                            {
                                Name = pair.Key,
                                Value = pair.Value
                            })
                            .ToList()
                    }
                )
                .ToList();

            if (iterations.Count > 0)
            {
                steps = AddParametersToSteps(steps, parameters[0].Keys);
            }

            var tmsTestCase = new TestCase
            {
                Id = testCaseGuid,
                Description = testCase.Description,
                State = StateType.NotReady,
                Priority = ConvertPriority(testCase.Priority),
                Steps = steps,
                PreconditionSteps = new List<Step>(),
                PostconditionSteps = new List<Step>(),
                Duration = _duration,
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
                Iterations = iterations,
                Links = links,
                Name = testCase.Title,
                SectionId = sectionId
            };

            _logger.LogDebug("Converted test case: {@TestCase}", tmsTestCase);

            testCases.Add(tmsTestCase);
        }

        _logger.LogInformation("Exported test cases");

        return testCases;
    }

    private static List<Step> AddParametersToSteps(List<Step> steps, IEnumerable<string> parameters)
    {
        foreach (var parameter in parameters)
        {
            steps.ForEach(s =>
            {
                s.Action = s.Action.Replace($"@{parameter}", $"<<<{parameter}>>>");
                s.Expected = s.Expected.Replace($"@{parameter}", $"<<<{parameter}>>>");
            });
        }

        return steps;
    }
}
