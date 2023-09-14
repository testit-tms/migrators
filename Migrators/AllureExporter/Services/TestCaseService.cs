using System.Text;
using AllureExporter.Client;
using AllureExporter.Models;
using Microsoft.Extensions.Logging;
using Models;
using Constants = AllureExporter.Models.Constants;

namespace AllureExporter.Services;

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

    public async Task<List<TestCase>> ConvertTestCases(int projectId, Dictionary<string, Guid> attributes,
        Dictionary<int, Guid> sectionIdMap)
    {
        _logger.LogInformation("Converting test cases");

        var testCases = new List<TestCase>();
        foreach (var section in sectionIdMap)
        {
            List<int> ids;
            if (section.Key == Constants.MainSectionId)
            {
                ids = await _client.GetTestCaseIdsFromMainSuite(projectId);
            }
            else
            {
                ids = await _client.GetTestCaseIdsFromSuite(projectId, section.Key);
            }

            foreach (var testCaseId in ids)
            {
                var testCase = await ConvertTestCase(testCaseId, section.Value, attributes);

                testCases.Add(testCase);
            }
        }

        _logger.LogInformation("Ending converting test cases");

        return testCases;
    }

    protected virtual async Task<List<Step>> ConvertSteps(int testCaseId)
    {
        var steps = await _client.GetSteps(testCaseId);

        _logger.LogDebug("Found steps: {@Steps}", steps);

        return steps.Select(allureStep =>
            {
                var attachments = new List<string>();

                foreach (var allureStepStep in allureStep.Steps.Where(allureStepStep =>
                             allureStepStep.Attachments != null))
                {
                    attachments.AddRange(allureStepStep.Attachments!.Select(a => a.Name));
                }

                var step = new Step
                {
                    Action = GetStepAction(allureStep),
                    Attachments = allureStep.Attachments != null
                        ? allureStep.Attachments.Select(a => a.Name).ToList()
                        : new List<string>(),
                    Expected = allureStep.ExpectedResult
                };

                step.Attachments.AddRange(attachments);

                return step;
            })
            .ToList();
    }

    private static string GetStepAction(AllureStep step)
    {
        var builder = new StringBuilder();

        if (!string.IsNullOrEmpty(step.Keyword))
        {
            builder.AppendLine($"<p>{step.Keyword}</p>");
        }

        builder.AppendLine($"<p>{step.Name}</p>");

        step.Steps
            .ForEach(s =>
            {
                if (!string.IsNullOrEmpty(s.Keyword))
                {
                    builder.AppendLine($"<p>{s.Keyword}</p>");
                }

                builder.AppendLine($"<p>{s.Name}</p>");
            });

        return builder.ToString();
    }

    protected virtual async Task<TestCase> ConvertTestCase(int testCaseId, Guid sectionId, Dictionary<string, Guid> attributes)
    {
        var testCase = await _client.GetTestCaseById(testCaseId);

        _logger.LogDebug("Found test case: {@TestCase}", testCase);

        var links = await _client.GetLinks(testCaseId);

        _logger.LogDebug("Found links: {@Links}", links);

        var testCaseGuid = Guid.NewGuid();
        var tmsAttachments = await _attachmentService.DownloadAttachments(testCaseId, testCaseGuid);
        var steps = await ConvertSteps(testCaseId);

        var allureTestCase = new TestCase
        {
            Id = testCaseGuid,
            Name = testCase.Name,
            Description = testCase.Description,
            State = StateType.NotReady,
            Priority = PriorityType.Medium,
            PreconditionSteps = new List<Step>(),
            PostconditionSteps = new List<Step>(),
            Tags = testCase.Tags.Select(t => t.Name).ToList(),
            Iterations = new List<Iteration>(),
            SectionId = sectionId,
            Links = links.Select(l => new Link
            {
                Url = l.Url,
                Title = l.Name,
            }).ToList(),
            Attributes = new List<CaseAttribute>
            {
                new()
                {
                    Id = attributes[Constants.AllureStatus],
                    Value = testCase.Status.Name
                },
                new()
                {
                    Id = attributes[Constants.AllureTestLayer],
                    Value = testCase.Layer != null ? testCase.Layer.Name : string.Empty
                }
            },
            Attachments = tmsAttachments,
            Steps = steps
        };

        var customFields = await _client.GetCustomFieldsFromTestCase(testCaseId);

        foreach (var attribute in attributes)
        {
            if (attribute.Key == Constants.AllureStatus || attribute.Key == Constants.AllureTestLayer)
            {
                continue;
            }

            var customField = customFields.FirstOrDefault(cf => cf.CustomField.Name == attribute.Key);

            if (customField != null)
            {
                allureTestCase.Attributes.Add(new CaseAttribute
                {
                    Id = attribute.Value,
                    Value = customField.Name
                });
            }
            else
            {
                allureTestCase.Attributes.Add(new CaseAttribute
                {
                    Id = attribute.Value,
                    Value = string.Empty
                });
            }
        }

        _logger.LogDebug("Converted test case: {@TestCase}", allureTestCase);

        return allureTestCase;
    }
}
