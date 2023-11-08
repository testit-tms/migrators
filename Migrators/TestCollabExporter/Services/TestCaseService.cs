using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Models;
using TestCollabExporter.Client;
using TestCollabExporter.Models;

namespace TestCollabExporter.Services;

public class TestCaseService : ITestCaseService
{
    private readonly ILogger<TestCaseService> _logger;
    private readonly IClient _client;
    private readonly IAttachmentService _attachmentService;

    public TestCaseService(ILogger<TestCaseService> logger, IClient client, IAttachmentService attachmentService)
    {
        _logger = logger;
        _client = client;
        _attachmentService = attachmentService;
    }

    public async Task<List<TestCase>> ConvertTestCases(int projectId, Dictionary<int, Guid> sectionMap,
        Dictionary<string, Guid> attributes, Dictionary<int, Guid> sharedStepsMap)
    {
        _logger.LogInformation("Converting test cases");

        var testCases = new List<TestCase>();

        foreach (var section in sectionMap)
        {
            var testCasesFromSection = await _client.GetTestCases(projectId, section.Key);

            foreach (var testCollabTestCase in testCasesFromSection)
            {
                var testCaseId = Guid.NewGuid();
                var descriptionData = ConvertDescription(testCollabTestCase.Description);
                var attachments = new List<string>();

                foreach (var attachment in testCollabTestCase.Attachments)
                {
                    var attachmentUrl =
                        await _attachmentService.DownloadAttachment(testCaseId, attachment.Url, attachment.Name);
                    attachments.Add(attachmentUrl);
                }

                foreach (var attachment in descriptionData.Attachments)
                {
                    var attachmentUrl =
                        await _attachmentService.DownloadAttachment(testCaseId, attachment.Url, attachment.Name);
                    attachments.Add(attachmentUrl);
                }

                var testCase = new TestCase
                {
                    Id = testCaseId,
                    Name = testCollabTestCase.Title,
                    Description = descriptionData.Description,
                    SectionId = section.Value,
                    State = StateType.NotReady,
                    Priority = ConvertPriority(testCollabTestCase.Priority),
                    PreconditionSteps = new List<Step>(),
                    PostconditionSteps = new List<Step>(),
                    Steps = ConvertSteps(testCollabTestCase.Steps, sharedStepsMap),
                    Duration = testCollabTestCase.ExecutionTime == 0 ? 10000 : testCollabTestCase.ExecutionTime,
                    Attributes = ConvertAttributes(testCollabTestCase.CustomFields, attributes),
                    Tags = testCollabTestCase.Tags.Select(t => t.Name).ToList(),
                    Attachments = attachments,
                    Iterations = new List<Iteration>(),
                    Links = new List<Link>()
                };

                testCases.Add(testCase);
            }
        }

        return testCases;
    }


    private static PriorityType ConvertPriority(string priority)
    {
        return priority switch
        {
            "0" => PriorityType.Low,
            "1" => PriorityType.Medium,
            "2" => PriorityType.High,
            _ => PriorityType.Medium
        };
    }

    private static List<Step> ConvertSteps(List<Steps> stepsList, IReadOnlyDictionary<int, Guid> sharedStepsMap)
    {
        var steps = new List<Step>();

        foreach (var step in stepsList)
        {
            if (step.ReusableStepId != null)
            {
                steps.Add(new Step
                {
                    Action = string.Empty,
                    Expected = string.Empty,
                    TestData = string.Empty,
                    ActionAttachments = new List<string>(),
                    ExpectedAttachments = new List<string>(),
                    TestDataAttachments = new List<string>(),
                    SharedStepId = sharedStepsMap[(int)step.ReusableStepId]
                });

                continue;
            }

            steps.Add(new Step
            {
                Action = step.Step,
                Expected = step.ExpectedResult,
                TestData = string.Empty,
                ActionAttachments = new List<string>(),
                ExpectedAttachments = new List<string>(),
                TestDataAttachments = new List<string>()
            });
        }

        return steps;
    }

    private static List<CaseAttribute> ConvertAttributes(IReadOnlyCollection<CustomField>? customFields,
        Dictionary<string, Guid> attributesMap)
    {
        var attributes = new List<CaseAttribute>();

        if (customFields == null)
        {
            return attributesMap.Select(a => new CaseAttribute
            {
                Id = a.Value,
                Value = string.Empty
            }).ToList();
        }

        foreach (var attribute in attributesMap)
        {
            var customField = customFields.FirstOrDefault(cf => cf.Name == attribute.Key);

            if (customField == null)
            {
                attributes.Add(new CaseAttribute
                {
                    Id = attribute.Value,
                    Value = string.Empty
                });
                continue;
            }

            attributes.Add(new CaseAttribute
            {
                Id = attribute.Value,
                Value = customField.Value.ToString()!
            });
        }

        return attributes;
    }


    private static DescriptionData ConvertDescription(string description)
    {
        const string pattern = """<div class="attachment">.*?</div>\s*</div>""";
        var matches = Regex.Matches(description, pattern, RegexOptions.Singleline);

        if (matches.Count == 0)
        {
            return new DescriptionData
            {
                Description = description,
                Attachments = new List<Attachments>()
            };
        }

        var attachments = new List<Attachments>();
        foreach (Match match in matches)
        {
            var attachment = match.Value;
            var attachmentUrl = Regex.Match(attachment, """
                                                        <img src="(.*?)"
                                                        """).Groups[1].Value;

            attachments.Add(new Attachments
            {
                Name = attachmentUrl.Split('/').Last(),
                Url = attachmentUrl
            });

            description = description.Replace(attachment, string.Empty);
        }

        return new DescriptionData
        {
            Description = description,
            Attachments = attachments
        };
    }
}
