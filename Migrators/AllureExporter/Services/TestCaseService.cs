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
    private readonly IStepService _stepService;

    public TestCaseService(ILogger<TestCaseService> logger, IClient client, IAttachmentService attachmentService,
        IStepService stepService)
    {
        _logger = logger;
        _client = client;
        _attachmentService = attachmentService;
        _stepService = stepService;
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

    protected virtual async Task<TestCase> ConvertTestCase(int testCaseId, Guid sectionId,
        Dictionary<string, Guid> attributes)
    {
        var testCase = await _client.GetTestCaseById(testCaseId);

        _logger.LogDebug("Found test case: {@TestCase}", testCase);

        var links = await _client.GetLinks(testCaseId);

        _logger.LogDebug("Found links: {@Links}", links);

        var testCaseGuid = Guid.NewGuid();
        var tmsAttachments = await _attachmentService.DownloadAttachments(testCaseId, testCaseGuid);
        var preconditionSteps = testCase.Precondition != null ? [new Step { Action = testCase.Precondition }] : new List<Step>();
        var steps = await _stepService.ConvertSteps(testCaseId);
        var caseAttributes = await ConvertAttributes(testCaseId, testCase, attributes);

        var allureTestCase = new TestCase
        {
            Id = testCaseGuid,
            Name = testCase.Name,
            Description = testCase.Description,
            State = StateType.NotReady,
            Priority = PriorityType.Medium,
            PreconditionSteps = preconditionSteps,
            PostconditionSteps = new List<Step>(),
            Tags = testCase.Tags.Select(t => t.Name).ToList(),
            Iterations = new List<Iteration>(),
            SectionId = sectionId,
            Links = links.Select(l => new Link
            {
                Url = l.Url,
                Title = l.Name,
            }).ToList(),
            Attributes = caseAttributes,
            Attachments = tmsAttachments,
            Steps = steps
        };

        _logger.LogDebug("Converted test case: {@TestCase}", allureTestCase);

        return allureTestCase;
    }

    private async Task<List<CaseAttribute>> ConvertAttributes(int testCaseId, AllureTestCase testCase,
        Dictionary<string, Guid> attributes)
    {
        var caseAttributes = new List<CaseAttribute>
        {
            new CaseAttribute
            {
                Id = attributes[Constants.AllureStatus],
                Value = testCase.Status.Name
            },
            new CaseAttribute
            {
                Id = attributes[Constants.AllureTestLayer],
                Value = testCase.Layer != null ? testCase.Layer.Name : string.Empty
            }
        };

        var customFields = await _client.GetCustomFieldsFromTestCase(testCaseId);

        foreach (var attribute in attributes)
        {
            if (attribute.Key is Constants.AllureStatus or Constants.AllureTestLayer)
            {
                continue;
            }

            var customField = customFields.FirstOrDefault(cf => cf.CustomField.Name == attribute.Key);

            if (customField != null)
            {
                caseAttributes.Add(new CaseAttribute
                {
                    Id = attribute.Value,
                    Value = customField.Name
                });
            }
            else
            {
                caseAttributes.Add(new CaseAttribute
                {
                    Id = attribute.Value,
                    Value = string.Empty
                });
            }
        }

        return caseAttributes;
    }
}
