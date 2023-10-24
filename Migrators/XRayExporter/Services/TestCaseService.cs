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

    private const int DefaultDuration = 10000;

    public TestCaseService(ILogger<TestCaseService> logger, IClient client, IAttachmentService attachmentService)
    {
        _logger = logger;
        _client = client;
        _attachmentService = attachmentService;
        _attributeMap = new Dictionary<string, Attribute>();
    }

    public async Task<TestCaseData> ConvertTestCases(Dictionary<int, Guid> sectionMap)
    {
        _logger.LogInformation("Converting test cases");

        InitializeAttributes();

        var testCases = new List<TestCase>();
        var sharedSteps = new List<SharedStep>();

        foreach (var section in sectionMap)
        {
            var testCasesFromFolder = await _client.GetTestFromFolder(section.Key);

            foreach (var test in testCasesFromFolder)
            {
                var testCase = await _client.GetTest(test.Key);
                var item = await _client.GetItem(testCase.Self);
                var testCaseId = Guid.NewGuid();
                var steps = await ConvertStep(testCaseId, testCase);
                var attachments = await ConvertAttachments(testCaseId, item.Fields.Attachments);

                var newTestCase = new TestCase
                {
                    Id = testCaseId,
                    Name = item.Fields.Summary,
                    Description = item.Fields.Description,
                    Steps = steps,
                    Attributes = GetAttributes(testCase),
                    PreconditionSteps = ConvertPreconditionSteps(testCase.Preconditions),
                    PostconditionSteps = new List<Step>(),
                    Attachments = attachments,
                    Duration = DefaultDuration,
                    State = StateType.NotReady,
                    Priority = PriorityType.Medium,
                    SectionId = section.Value,
                    Tags = item.Fields.Labels,
                    Iterations = new List<Iteration>(),
                    Links = new List<Link>()
                };

                testCases.Add(newTestCase);
            }
        }

        return new TestCaseData
        {
            TestCases = testCases,
            SharedSteps = sharedSteps,
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
            Options = new List<string>()
        };

        _attributeMap[Constants.XrayType] = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.XrayType,
            Type = AttributeType.Options,
            Options = new List<string>()
        };

        _attributeMap[Constants.XrayStatus] = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.XrayStatus,
            Type = AttributeType.Options,
            Options = new List<string>()
        };

        _attributeMap[Constants.XrayArchived] = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.XrayArchived,
            Type = AttributeType.Options,
            Options = new List<string>()
        };
    }

    private List<CaseAttribute> GetAttributes(XRayTestFull test)
    {
        var attributes = new List<CaseAttribute>();

        if (!_attributeMap[Constants.XrayType].Options
                .Any(o => o.Equals(test.Type, StringComparison.InvariantCultureIgnoreCase)))
        {
            _attributeMap[Constants.XrayType].Options.Add(test.Type);
        }

        attributes.Add(new CaseAttribute
        {
            Id = _attributeMap[Constants.XrayType].Id,
            Value = test.Type
        });

        if (!_attributeMap[Constants.XrayStatus].Options
                .Any(o => o.Equals(test.Status, StringComparison.InvariantCultureIgnoreCase)))
        {
            _attributeMap[Constants.XrayStatus].Options.Add(test.Status);
        }

        attributes.Add(new CaseAttribute
        {
            Id = _attributeMap[Constants.XrayStatus].Id,
            Value = test.Status
        });

        if (!_attributeMap[Constants.XrayArchived].Options
                .Any(o => o.Equals(test.Archived.ToString(), StringComparison.InvariantCultureIgnoreCase)))
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
            }
        }

        return steps;
    }

    private static List<Step> ConvertPreconditionSteps(IEnumerable<Precondition> preconditions)
    {
        return preconditions.Select(p => new Step()
        {
            Action = p.Condition,
            Expected = string.Empty,
            TestData = string.Empty,
            ActionAttachments = new List<string>(),
            ExpectedAttachments = new List<string>(),
            TestDataAttachments = new List<string>()
        }).ToList();
    }

    private async Task<List<string>> ConvertAttachments(Guid testCaseId, List<Attachment> attachments)
    {
        var attachmentNames = new List<string>();

        foreach (var attachment in attachments)
        {
            var attachmentName =
                await _attachmentService.DownloadAttachment(testCaseId, attachment.Content, attachment.Filename);
            attachmentNames.Add(attachmentName);
        }

        return attachmentNames;
    }
}
