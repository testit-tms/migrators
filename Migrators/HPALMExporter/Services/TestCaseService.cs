using HPALMExporter.Client;
using HPALMExporter.Models;
using Microsoft.Extensions.Logging;
using Models;

namespace HPALMExporter.Services;

public class TestCaseService : ITestCaseService
{
    private readonly ILogger<TestCaseService> _logger;
    private readonly IClient _client;
    private readonly IAttachmentService _attachmentService;
    private readonly Dictionary<int, TestCase> _testCases;
    private readonly Dictionary<int, SharedStep> _sharedSteps;

    private const int DefaultDuration = 10000;

    public TestCaseService(ILogger<TestCaseService> logger, IClient client, IAttachmentService attachmentService)
    {
        _logger = logger;
        _client = client;
        _attachmentService = attachmentService;
        _testCases = new Dictionary<int, TestCase>();
        _sharedSteps = new Dictionary<int, SharedStep>();
    }

    public async Task<TestCaseData> ConvertTestCases(Dictionary<int, Guid> sectionMap,
        Dictionary<string, Guid> attributeMap)
    {
        _logger.LogInformation("Convert test cases from HP ALM");

        foreach (var section in sectionMap)
        {
            var tests = await _client.GetTests(section.Key, attributeMap.Keys.ToList());

            foreach (var test in tests)
            {
                if (_testCases.ContainsKey(test.Id) || _sharedSteps.ContainsKey(test.Id))
                {
                    continue;
                }

                var testCaseId = Guid.NewGuid();
                var attributes = ConvertAttributes(test.Attrubites, attributeMap);
                var iterations = await ConvertParameters(test.Id);
                var attachmentData = await _attachmentService.ConvertAttachmentsFromTest(testCaseId, test.Id);
                var steps = await ConvertStep(test.Id, testCaseId, attributeMap, sectionMap);

                var testCase = new TestCase
                {
                    Id = testCaseId,
                    Name = test.Name,
                    Description = test.Description,
                    SectionId = section.Value,
                    State = StateType.NotReady,
                    Priority = PriorityType.Medium,
                    Duration = DefaultDuration,
                    Tags = new List<string>(),
                    PostconditionSteps = new List<Step>(),
                    PreconditionSteps = new List<Step>(),
                    Attributes = attributes,
                    Iterations = iterations,
                    Attachments = attachmentData.Attachments,
                    Links = attachmentData.Links,
                    Steps = steps
                };

                _testCases.Add(test.Id, testCase);
            }
        }

        return new TestCaseData
        {
            TestCases = _testCases.Values.ToList(),
            SharedSteps = _sharedSteps.Values.ToList()
        };
    }

    private static List<CaseAttribute> ConvertAttributes(Dictionary<string, string> attributesOfTest,
        Dictionary<string, Guid> attributeMap)
    {
        var caseAttributes = new List<CaseAttribute>();

        foreach (var attribute in attributeMap)
        {
            var value = attributesOfTest.SingleOrDefault(a => a.Key == attribute.Key).Value;

            caseAttributes.Add(new CaseAttribute
            {
                Id = attribute.Value,
                Value = value ?? string.Empty
            });
        }

        return caseAttributes;
    }

    private async Task<List<Iteration>> ConvertParameters(int testId)
    {
        _logger.LogInformation("Convert parameters from HP ALM for test {TestId}", testId);

        var parameters = await _client.GetParameters(testId);
        var iterations = new List<Iteration>();

        if (!parameters.Any())
        {
            return iterations;
        }

        var iteration = new Iteration
        {
            Parameters = parameters.Select(p =>
                    new Parameter
                    {
                        Name = p.Name,
                        Value = p.Value
                    })
                .ToList()
        };

        iterations.Add(iteration);

        return iterations;
    }

    private async Task<List<Step>> ConvertStep(int testId, Guid testCaseId, Dictionary<string, Guid> attributeMap,
        Dictionary<int, Guid> sectionMap)
    {
        _logger.LogInformation("Convert steps from HP ALM for test {TestId}", testId);

        var steps = await _client.GetSteps(testId);

        var convertedSteps = new List<Step>(steps.Count);

        foreach (var step in steps)
        {
            if (step.LinkId == null)
            {
                var convertedStep = new Step
                {
                    Action = step.Description,
                    Expected = step.Expected,
                    TestData = string.Empty,
                    ActionAttachments = new List<string>(),
                    ExpectedAttachments = new List<string>(),
                    TestDataAttachments = new List<string>()
                };

                if (step.HasAttachments)
                {
                    var attachmentData = await _attachmentService.ConvertAttachmentsFromStep(testCaseId, step.Id);

                    convertedStep.TestDataAttachments = attachmentData.Attachments;

                    attachmentData.Links.ForEach(l =>
                        convertedStep.TestData += $"<a href=\"{l.Description}\">{l.Title}</a>\n");
                }

                convertedSteps.Add(convertedStep);
            }
            else
            {
                if (_sharedSteps.TryGetValue(step.LinkId.Value, out var existedSharedStep))
                {
                    var convertedStep = new Step
                    {
                        Action = string.Empty,
                        Expected = string.Empty,
                        TestData = string.Empty,
                        ActionAttachments = new List<string>(),
                        ExpectedAttachments = new List<string>(),
                        TestDataAttachments = new List<string>(),
                        SharedStepId = existedSharedStep.Id
                    };

                    convertedSteps.Add(convertedStep);
                }
                else if (_testCases.TryGetValue(step.LinkId.Value, out var existedTestCase))
                {
                    var sharedStep = new SharedStep
                    {
                        Id = existedTestCase.Id,
                        Name = existedTestCase.Name,
                        Description = existedTestCase.Description,
                        State = StateType.NotReady,
                        Priority = PriorityType.Medium,
                        Steps = existedTestCase.Steps,
                        Attributes = existedTestCase.Attributes,
                        Attachments = existedTestCase.Attachments,
                        Links = existedTestCase.Links,
                        SectionId = existedTestCase.SectionId,
                        Tags = new List<string>()
                    };

                    _sharedSteps.Add(step.LinkId.Value, sharedStep);
                    _testCases.Remove(step.LinkId.Value);

                    var convertedStep = new Step
                    {
                        Action = string.Empty,
                        Expected = string.Empty,
                        TestData = string.Empty,
                        ActionAttachments = new List<string>(),
                        ExpectedAttachments = new List<string>(),
                        TestDataAttachments = new List<string>(),
                        SharedStepId = existedTestCase.Id
                    };

                    convertedSteps.Add(convertedStep);
                }
                else
                {
                    var sharedStepId = Guid.NewGuid();
                    var test = await _client.GetTest(step.LinkId.Value, attributeMap.Keys.ToList());
                    var childrenSteps = await ConvertStep(test.Id, sharedStepId, attributeMap, sectionMap);
                    var attachmentData = await _attachmentService.ConvertAttachmentsFromTest(sharedStepId, test.Id);

                    var sharedStep = new SharedStep
                    {
                        Id = sharedStepId,
                        Name = test.Name,
                        Description = test.Description,
                        State = StateType.NotReady,
                        Priority = PriorityType.Medium,
                        Steps = childrenSteps,
                        Attributes = ConvertAttributes(test.Attrubites, attributeMap),
                        Attachments = attachmentData.Attachments,
                        Links = attachmentData.Links,
                        SectionId = sectionMap[test.ParentId],
                        Tags = new List<string>()
                    };

                    _sharedSteps.Add(step.LinkId.Value, sharedStep);

                    var convertedStep = new Step
                    {
                        Action = string.Empty,
                        Expected = string.Empty,
                        TestData = string.Empty,
                        ActionAttachments = new List<string>(),
                        ExpectedAttachments = new List<string>(),
                        TestDataAttachments = new List<string>(),
                        SharedStepId = sharedStepId
                    };

                    convertedSteps.Add(convertedStep);
                }
            }
        }

        return convertedSteps;
    }
}
