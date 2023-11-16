using Microsoft.Extensions.Logging;
using Models;
using PractiTestExporter.Client;
using PractiTestExporter.Models;
using Constants = PractiTestExporter.Models.Constants;

namespace PractiTestExporter.Services;

public class TestCaseService : ITestCaseService
{
    private readonly ILogger<TestCaseService> _logger;
    private readonly IClient _client;
    private readonly IAttachmentService _attachmentService;
    private readonly Dictionary<string, SharedStep> _sharedSteps;
    private readonly Dictionary<string, TestCase> _testCases;

    public TestCaseService(ILogger<TestCaseService> logger, IClient client, IAttachmentService attachmentService)
    {
        _logger = logger;
        _client = client;
        _attachmentService = attachmentService;
        _sharedSteps = new Dictionary<string, SharedStep>();
        _testCases = new Dictionary<string, TestCase>();
    }

    public async Task<TestCaseData> ConvertTestCases(Guid sectionId, Dictionary<string, Guid> attributeMap)
    {
        _logger.LogInformation("Converting test cases");

        var practiTestTestCases = await _client.GetTestCases();

        _logger.LogDebug("Found {Count} test cases", practiTestTestCases.Count);

        foreach (var practiTestTestCase in practiTestTestCases)
        {
            if (_sharedSteps.TryGetValue(practiTestTestCase.Id, out var sharedStep))
            {
                sharedStep.SectionId = sectionId;
                continue;
            }

            _testCases.Add(
                practiTestTestCase.Id,
                await ConvertTestCases(
                    practiTestTestCase,
                    sectionId,
                    attributeMap
                )
            );
        }

        _logger.LogInformation("Exported test cases");

        return new TestCaseData
        {
            TestCases = _testCases.Values.ToList(),
            SharedSteps = _sharedSteps.Values.ToList()
        };
    }

    private async Task<TestCase> ConvertTestCases(
        PractiTestTestCase practiTestTestCase,
        Guid sectionId,
        Dictionary<string, Guid> attributeMap)
    {
        var testCaseId = Guid.NewGuid();
        var attributes = ConvertAttributes(practiTestTestCase.Attributes.CustomFields, attributeMap);
        var attachments = await _attachmentService.DownloadAttachments(
            Constants.TestCaseEntityType,
            practiTestTestCase.Id,
            testCaseId
        );
        var steps = await ConvertSteps(practiTestTestCase.Id, testCaseId, attributeMap);

        steps.ForEach(step =>
            attachments.AddRange(step.ActionAttachments)
        );

        return new TestCase
        {
            Id = testCaseId,
            Description = practiTestTestCase.Attributes.Description,
            State = StateType.NotReady,
            Priority = PriorityType.Medium,
            Steps = steps,
            PreconditionSteps = ConvertPreconditionSteps(practiTestTestCase.Attributes.Preconditions),
            PostconditionSteps = new List<Step>(),
            Duration = Constants.Duration,
            Attributes = attributes,
            Tags = practiTestTestCase.Attributes.Tags,
            Attachments = attachments,
            Iterations = new List<Iteration>(),
            Links = new List<Link>(),
            Name = practiTestTestCase.Attributes.Name,
            SectionId = sectionId,
        };
    }

    private static List<Step> ConvertPreconditionSteps(string preconditions)
    {
        return string.IsNullOrEmpty(preconditions)
            ? new List<Step>()
            : new List<Step> {
                new Step
                {
                    Action = preconditions,
                    Expected = string.Empty,
                    TestData = string.Empty,
                    ActionAttachments = new List<string>(),
                    ExpectedAttachments = new List<string>(),
                    TestDataAttachments = new List<string>()
                }
            };
    }

    private async Task<Guid> ConvertSharedStep(string itemKey, Dictionary<string, Guid> attributeMap)
    {
        if (_sharedSteps.TryGetValue(itemKey, out var step))
        {
            return step.Id;
        }

        if (_testCases.TryGetValue(itemKey, out var existingTestCase))
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

            _testCases.Remove(itemKey);
            _sharedSteps.Add(itemKey, convertedSharedStep);

            return convertedSharedStep.Id;
        }

        var practiTestTestCase = await _client.GetTestCaseById(itemKey);
        var testCaseId = Guid.NewGuid();
        var steps = await ConvertSteps(itemKey, testCaseId, attributeMap);
        var attachments = await _attachmentService.DownloadAttachments(
            Constants.TestCaseEntityType,
            practiTestTestCase.Id,
            testCaseId
        );

        steps.ForEach(step =>
            attachments.AddRange(step.ActionAttachments)
        );

        var sharedStep = new SharedStep
        {
            Id = testCaseId,
            Name = practiTestTestCase.Attributes.Name,
            Description = practiTestTestCase.Attributes.Description,
            Steps = steps,
            Attributes = ConvertAttributes(practiTestTestCase.Attributes.CustomFields, attributeMap),
            Attachments = attachments,
            State = StateType.NotReady,
            Priority = PriorityType.Medium,
            Tags = practiTestTestCase.Attributes.Tags,
            Links = new List<Link>()
        };

        _sharedSteps.Add(itemKey, sharedStep);

        return testCaseId;
    }

    private async Task<List<Step>> ConvertSteps(
        string practiTestCaseId,
        Guid testCaseId,
        Dictionary<string, Guid> attributeMap)
    {
        var practiTestSteps = await _client.GetStepsByTestCaseId(practiTestCaseId);

        _logger.LogDebug("Found steps: {@Steps}", practiTestSteps);

        var steps = new List<Step>();

        foreach (var practiTestStep in practiTestSteps)
        {
            if (practiTestStep.Attributes.TestToCallId != null)
            {
                var sharedStepId = await ConvertSharedStep(practiTestStep.Attributes.TestToCallId.ToString(), attributeMap);

                steps.Add(new Step
                {
                    SharedStepId = sharedStepId,
                    Action = string.Empty,
                    Expected = string.Empty,
                    TestData = string.Empty,
                    ActionAttachments = new List<string>(),
                    ExpectedAttachments = new List<string>(),
                    TestDataAttachments = new List<string>()
                });

                continue;
            }

            steps.Add(
                new Step
                {
                    Action = practiTestStep.Attributes.Name,
                    Expected = practiTestStep.Attributes.ExpectedResults,
                    TestData = string.Empty,
                    ActionAttachments = new List<string>(),
                    ExpectedAttachments = new List<string>(),
                    TestDataAttachments = new List<string>(),
                }
            );

            var attachmentNames = await _attachmentService.DownloadAttachments(
                Constants.StepEntityType,
                practiTestStep.Id,
                testCaseId);

            foreach (var attachmentName in attachmentNames)
            {
                steps.Last().ActionAttachments.Add(attachmentName);
                steps.Last().Action += $"<br><p><<<{attachmentName}>>></p>";
            }
        }
        _logger.LogDebug("Converted steps: {@Steps}", steps);

        return steps;
    }

    private List<CaseAttribute> ConvertAttributes(
        Dictionary<string, string> customFields,
        Dictionary<string, Guid> attributeMap)
    {
        var attributes = new List<CaseAttribute>();

        foreach (var customField in customFields)
        {
            attributes.Add(
                new CaseAttribute
                {
                    Id = attributeMap[ConvertCustomFieldKey(customField.Key)],
                    Value = customField.Value == null ? string.Empty : customField.Value.ToString()
                }
            );
        }

        return attributes;
    }

    private static string ConvertCustomFieldKey(string key)
    {
        return key.Replace("---f-", string.Empty);
    }
}
