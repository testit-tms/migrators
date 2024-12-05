using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Models;
using XRayExporter.Client;
using XRayExporter.Models;
using Attribute = Models.Attribute;
using Constants = XRayExporter.Models.Constants;
using Step = Models.Step;

namespace XRayExporter.Services;

public class TestCaseService : ITestCaseService
{
    private readonly ILogger<TestCaseService> _logger;
    private readonly IClient _client;
    private readonly IAttachmentService _attachmentService;
    private readonly Dictionary<string, Attribute> _attributeMap;
    private readonly Dictionary<string, SharedStep> _sharedSteps;
    private readonly Dictionary<string, TestCase> _testCases;

    private const int DefaultDuration = 10000;
    private const string SharedStepMark = "This step was calling test issue";
    private const string SharedStepRegex = "href=\"([^\"]+)\"";

    public TestCaseService(ILogger<TestCaseService> logger, IClient client, IAttachmentService attachmentService)
    {
        _logger = logger;
        _client = client;
        _attachmentService = attachmentService;
        _attributeMap = new Dictionary<string, Attribute>();
        _sharedSteps = new Dictionary<string, SharedStep>();
        _testCases = new Dictionary<string, TestCase>();
    }

    public async Task<TestCaseData> ConvertTestCases(Dictionary<int, Guid> sectionMap)
    {
        _logger.LogInformation("Converting test cases");

        InitializeAttributes();

        foreach (var section in sectionMap)
        {
            var testCasesFromFolder = await _client.GetTestFromFolder(section.Key);

            foreach (var test in testCasesFromFolder)
            {
                if (_sharedSteps.TryGetValue(test.Key, out var sharedStep))
                {
                    sharedStep.SectionId = section.Value;
                    continue;
                }

                var testCase = await _client.GetTest(test.Key);
                var item = await _client.GetItem(testCase.Self);
                var testCaseId = Guid.NewGuid();
                var steps = await ConvertStep(testCaseId, testCase);
                var attachments = await ConvertAttachments(testCaseId, item.Fields.Attachments);

                steps.ForEach(s =>
                    attachments.AddRange(s.ActionAttachments)
                );

                var newTestCase = new TestCase
                {
                    Id = testCaseId,
                    Name = item.Fields.Summary,
                    Description = item.Fields.Description,
                    Steps = steps,
                    Attributes = ConvertAttributes(testCase),
                    PreconditionSteps = ConvertPreconditionSteps(testCase.Preconditions),
                    PostconditionSteps = new List<Step>(),
                    Attachments = attachments,
                    Duration = DefaultDuration,
                    State = StateType.NotReady,
                    Priority = PriorityType.Medium,
                    SectionId = section.Value,
                    Tags = item.Fields.Labels,
                    Iterations = new List<Iteration>(),
                    Links = ConvertLink(item.Fields.IssueLinks)
                };

                _testCases.Add(test.Key, newTestCase);
            }
        }

        return new TestCaseData
        {
            TestCases = _testCases.Values.ToList(),
            SharedSteps = _sharedSteps.Values.ToList(),
            Attributes = _attributeMap.Values.ToList()
        };
    }

    private void InitializeAttributes()
    {
        _attributeMap[Constants.XrayReporter] = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.XrayReporter,
            Type = AttributeType.String,
            Options = new List<string>(),
            IsRequired = false,
            IsActive = true
        };

        _attributeMap[Constants.XrayType] = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.XrayType,
            Type = AttributeType.Options,
            Options = new List<string>(),
            IsRequired = false,
            IsActive = true
        };

        _attributeMap[Constants.XrayStatus] = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.XrayStatus,
            Type = AttributeType.Options,
            Options = new List<string>(),
            IsRequired = false,
            IsActive = true
        };

        _attributeMap[Constants.XrayArchived] = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.XrayArchived,
            Type = AttributeType.Options,
            Options = new List<string>(),
            IsRequired = false,
            IsActive = true
        };
    }

    private List<CaseAttribute> ConvertAttributes(XRayTestFull test)
    {
        var attributes = new List<CaseAttribute>();

        if (!_attributeMap[Constants.XrayType].Options
                .Any(o => o.Equals(test.Type)))
        {
            _attributeMap[Constants.XrayType].Options.Add(test.Type);
        }

        attributes.Add(new CaseAttribute
        {
            Id = _attributeMap[Constants.XrayType].Id,
            Value = test.Type
        });

        if (!_attributeMap[Constants.XrayStatus].Options
                .Any(o => o.Equals(test.Status)))
        {
            _attributeMap[Constants.XrayStatus].Options.Add(test.Status);
        }

        attributes.Add(new CaseAttribute
        {
            Id = _attributeMap[Constants.XrayStatus].Id,
            Value = test.Status
        });

        if (!_attributeMap[Constants.XrayArchived].Options
                .Any(o => o.Equals(test.Archived.ToString())))
        {
            _attributeMap[Constants.XrayArchived].Options.Add(test.Archived.ToString());
        }

        attributes.Add(new CaseAttribute
        {
            Id = _attributeMap[Constants.XrayArchived].Id,
            Value = test.Archived.ToString()
        });

        attributes.Add(new CaseAttribute
        {
            Id = _attributeMap[Constants.XrayReporter].Id,
            Value = test.Reporter
        });

        return attributes;
    }

    private async Task<List<Step>> ConvertStep(Guid testCaseId, XRayTestFull test)
    {
        var steps = new List<Step>();

        foreach (var step in test.Definition.Steps)
        {
            if (step.Step.Rendered.Contains(SharedStepMark))
            {
                var match = Regex.Match(step.Step.Rendered, SharedStepRegex);

                if (match.Success)
                {
                    var sharedStepId = await ConvertSharedStep(match.Groups[1].Value.Split("/").Last());

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
            }

            steps.Add(new Step
            {
                Action = step.Step.Rendered,
                Expected = step.Result.Rendered,
                TestData = step.Data.Rendered,
                ActionAttachments = new List<string>(),
                ExpectedAttachments = new List<string>(),
                TestDataAttachments = new List<string>()
            });

            foreach (var attachment in step.Attachments)
            {
                var attachmentName =
                    await _attachmentService.DownloadAttachment(testCaseId, attachment.FileURL, attachment.FileName);
                steps.Last().ActionAttachments.Add(attachmentName);
                steps.Last().Action += $"<br><p><<<{attachmentName}>>></p>";
            }
        }

        return steps;
    }

    private static List<Step> ConvertPreconditionSteps(IEnumerable<Precondition> preconditions)
    {
        return preconditions
            .Select(p => new Step
                {
                    Action = p.Condition,
                    Expected = string.Empty,
                    TestData = string.Empty,
                    ActionAttachments = new List<string>(),
                    ExpectedAttachments = new List<string>(),
                    TestDataAttachments = new List<string>()
                }
            )
            .ToList();
    }

    private async Task<List<string>> ConvertAttachments(Guid testCaseId, List<Attachment> attachments)
    {
        var attachmentNames = new List<string>();

        foreach (var attachment in attachments)
        {
            var attachmentName =
                await _attachmentService.DownloadAttachment(testCaseId, attachment.Content, attachment.FileName);
            attachmentNames.Add(attachmentName);
        }

        return attachmentNames;
    }

    private async Task<Guid> ConvertSharedStep(string itemKey)
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

        var testCase = await _client.GetTest(itemKey);
        var item = await _client.GetItem(testCase.Self);
        var testCaseId = Guid.NewGuid();
        var steps = await ConvertStep(testCaseId, testCase);
        var attachments = await ConvertAttachments(testCaseId, item.Fields.Attachments);

        var sharedStep = new SharedStep
        {
            Id = testCaseId,
            Name = item.Fields.Summary,
            Description = item.Fields.Description,
            Steps = steps,
            Attributes = ConvertAttributes(testCase),
            Attachments = attachments,
            State = StateType.NotReady,
            Priority = PriorityType.Medium,
            Tags = item.Fields.Labels,
            Links = new List<Link>()
        };

        _sharedSteps.Add(itemKey, sharedStep);

        return testCaseId;
    }

    private static List<Link> ConvertLink(IEnumerable<JiraLink> jiraLinks)
    {
        var links = new List<Link>();

        foreach (var jiraLink in jiraLinks)
        {
            var url = jiraLink.InwardIssue != null
                ? $"{jiraLink.InwardIssue.Self.Split("/rest").First()}/browse/{jiraLink.InwardIssue.Key}"
                : $"{jiraLink.OutwardIssue?.Self.Split("/rest").First()}/browse/{jiraLink.OutwardIssue?.Key}";

            var newLink = new Link
            {
                Title = jiraLink.Type.Name,
                Description = jiraLink.Type.Inward,
                Url = url
            };

            links.Add(newLink);
        }

        return links;
    }
}
