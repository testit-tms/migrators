using Microsoft.Extensions.Logging;
using Models;
using SpiraTestExporter.Client;
using SpiraTestExporter.Models;
using Constants = SpiraTestExporter.Models.Constants;

namespace SpiraTestExporter.Services;

public class TestCaseService : ITestCaseService
{
    private readonly ILogger<TestCaseService> _logger;
    private readonly IClient _client;
    private readonly IAttachmentService _attachmentService;
    private readonly Dictionary<int, SharedStep> _sharedSteps;
    private readonly Dictionary<int, TestCase> _testCases;
    public const int _duration = 10000;

    public TestCaseService(ILogger<TestCaseService> logger, IClient client, IAttachmentService attachmentService)
    {
        _logger = logger;
        _client = client;
        _attachmentService = attachmentService;
        _sharedSteps = new Dictionary<int, SharedStep>();
        _testCases = new Dictionary<int, TestCase>();
    }

    public async Task<TestCaseData> ConvertTestCases(int projectId, Dictionary<int, Guid> sectionMap,
        Dictionary<int, string> priorities, Dictionary<int, string> statuses, Dictionary<string, Guid> attributesMap)
    {
        _logger.LogInformation("Convert test cases");

        foreach (var section in sectionMap)
        {
            var testCases = await _client.GetTestFromFolder(projectId, section.Key);

            foreach (var testCase in testCases)
            {
                _logger.LogDebug("Convert test case {TestCaseId} {TestCaseName}", testCase.TestCaseId,
                    testCase.Name);

                var testCaseId = Guid.NewGuid();
                var attachments = await _attachmentService.GetAttachments(testCaseId, projectId, ArtifactType.TestCase,
                    testCase.TestCaseId);

                var steps = await ConvertSteps(projectId, testCaseId, testCase.TestCaseId, sectionMap, priorities,
                    statuses, attributesMap);
                steps.ForEach(s => attachments.AddRange(s.TestDataAttachments));

                var parameters = await _client.GetSpiraParameters(projectId, testCase.TestCaseId);

                var iterationParameters = parameters.Count != 0
                    ? new List<Iteration>
                    {
                        new()
                        {
                            Parameters = parameters.Select(p => new Parameter
                            {
                                Name = p.Name,
                                Value = string.IsNullOrEmpty(p.Value) ? string.Empty : p.Value
                            }).ToList()
                        }
                    }
                    : new List<Iteration>();

                var testCaseModel = new TestCase
                {
                    Id = testCaseId,
                    Name = testCase.Name,
                    Description = testCase.Description,
                    Priority = PriorityType.Medium,
                    State = StateType.NotReady,
                    SectionId = section.Value,
                    Steps = steps,
                    PreconditionSteps = new List<Step>(),
                    PostconditionSteps = new List<Step>(),
                    Duration = _duration,
                    Attributes = new List<CaseAttribute>
                    {
                        new()
                        {
                            Id = attributesMap[Constants.Priority],
                            Value = testCase.PriorityId.HasValue
                                ? priorities[testCase.PriorityId.Value]
                                : priorities[1]
                        },
                        new()
                        {
                            Id = attributesMap[Constants.Status],
                            Value = statuses[testCase.StatusId]
                        }
                    },
                    Tags = new List<string>(),
                    Attachments = attachments,
                    Iterations = iterationParameters,
                    Links = new List<Link>()
                };

                _testCases.Add(testCase.TestCaseId, testCaseModel);
            }
        }

        return new TestCaseData
        {
            TestCases = _testCases.Values.ToList(),
            SharedSteps = _sharedSteps.Values.ToList()
        };
    }

    private async Task<List<Step>> ConvertSteps(int projectId, Guid id, int testCaseId,
        Dictionary<int, Guid> sectionMap,
        Dictionary<int, string> priorities, Dictionary<int, string> statuses, Dictionary<string, Guid> attributesMap)
    {
        var steps = new List<Step>();

        _logger.LogDebug("Converting steps for test case {TestCaseId}", testCaseId);

        var spiraSteps = await _client.GetTestSteps(projectId, testCaseId);

        foreach (var spiraStep in spiraSteps)
        {
            if (spiraStep.Description.Equals("Call", StringComparison.InvariantCultureIgnoreCase))
            {
                var sharedStepId = await ConvertSharedStep(projectId, spiraStep.LinkedId!.Value, sectionMap, priorities,
                    statuses,
                    attributesMap);

                steps.Add(new Step
                {
                    Action = string.Empty,
                    Expected = string.Empty,
                    TestData = string.Empty,
                    ActionAttachments = new List<string>(),
                    ExpectedAttachments = new List<string>(),
                    TestDataAttachments = new List<string>(),
                    SharedStepId = sharedStepId
                });
            }
            else
            {
                var stepParameters = await _client.GetStepParameters(projectId, testCaseId, spiraStep.Id);
                var stepAttachments = await _attachmentService.GetAttachments(id, projectId,
                    ArtifactType.Step, spiraStep.Id);
                steps.Add(new Step
                {
                    Action = spiraStep.Description,
                    Expected = spiraStep.ExpectedResult,
                    TestData = stepParameters.Count != 0
                        ? stepParameters.Select(p => $"{p.Name}: {p.Value}").Aggregate((a, b) => $"{a}\n{b}")
                        : string.Empty,
                    ActionAttachments = new List<string>(),
                    ExpectedAttachments = new List<string>(),
                    TestDataAttachments = stepAttachments
                });
            }
        }

        return steps;
    }


    private async Task<Guid> ConvertSharedStep(int projectId, int stepId, Dictionary<int, Guid> sectionMap,
        Dictionary<int, string> priorities, Dictionary<int, string> statuses, Dictionary<string, Guid> attributesMap)
    {
        if (_sharedSteps.TryGetValue(stepId, out var step))
        {
            return step.Id;
        }

        if (_testCases.TryGetValue(stepId, out var existingTestCase))
        {
            var convertedSharedStep = new SharedStep
            {
                Id = existingTestCase.Id,
                Name = existingTestCase.Name,
                Description = existingTestCase.Description,
                Steps = existingTestCase.Steps,
                Attributes = existingTestCase.Attributes,
                Attachments = existingTestCase.Attachments,
                State = StateType.NotReady,
                Priority = PriorityType.Medium,
                Tags = existingTestCase.Tags,
                Links = existingTestCase.Links,
                SectionId = existingTestCase.SectionId
            };

            _testCases.Remove(stepId);
            _sharedSteps.Add(stepId, convertedSharedStep);

            return convertedSharedStep.Id;
        }

        var testCase = await _client.GetTestById(projectId, stepId);
        var testCaseId = Guid.NewGuid();
        var steps = await ConvertSteps(projectId, testCaseId, testCase.TestCaseId, sectionMap, priorities, statuses,
            attributesMap);
        var attachments = await _attachmentService.GetAttachments(testCaseId, projectId, ArtifactType.TestCase,
            testCase.TestCaseId);
        steps.ForEach(s => attachments.AddRange(s.TestDataAttachments));

        var sharedStep = new SharedStep
        {
            Id = testCaseId,
            Name = testCase.Name,
            Description = testCase.Description,
            Steps = steps,
            Attributes = new List<CaseAttribute>
            {
                new()
                {
                    Id = attributesMap[Constants.Priority],
                    Value = priorities[testCase.PriorityId!.Value],
                },
                new()
                {
                    Id = attributesMap[Constants.Status],
                    Value = statuses[testCase.StatusId]
                }
            },
            Attachments = attachments,
            State = StateType.NotReady,
            Priority = PriorityType.Medium,
            Tags = new List<string>(),
            Links = new List<Link>(),
            SectionId = sectionMap[testCase.FolderId!.Value]
        };

        _sharedSteps.Add(stepId, sharedStep);

        return testCaseId;
    }
}
