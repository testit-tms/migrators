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

    public async Task<List<TestCase>> ConvertTestCases(
        long projectId,
        Dictionary<string, Guid> sharedStepMap,
        Dictionary<string, Guid> attributes,
        SectionInfo sectionInfo)
    {
        var sectionIdMap = sectionInfo.SectionDictionary;
        _logger.LogInformation("Converting test cases");

        var testCases = new List<TestCase>();
        foreach (var section in sectionIdMap)
        {
            List<long> ids;
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
                var testCase = await ConvertTestCase(testCaseId, sharedStepMap, section.Value, attributes);
                ProcessFeatureSection(testCase, attributes, sectionInfo);
                testCases.Add(testCase);
            }
        }

        _logger.LogInformation("Ending converting test cases");

        return testCases;
    }

    private string GetAttributeNameById(Guid id, List<CaseAttribute> caseAttributes)
    {
        var attr = caseAttributes.FirstOrDefault(x =>
            x.Id == id);
        return attr?.Value.ToString() ?? string.Empty;
    }

    /// <summary>
    /// create feauture and story sections, attach testCase to the story;
    /// </summary>
    /// <returns>true if processed successfully</returns>
    protected virtual bool ProcessFeatureSection(TestCase testCase,
        Dictionary<string, Guid> attributes,
        SectionInfo sectionInfo)
    {
        string featureString = GetAttributeNameById(attributes[Constants.Feature], testCase.Attributes);
        string storyString = GetAttributeNameById(attributes[Constants.Story], testCase.Attributes);
        if (featureString == "" || storyString == "") return false;

        Section currentSection = Section.FindSection(s => s.Id == testCase.SectionId, sectionInfo.MainSection)!;

        var featureSection = Section.FindSection(
            s => s.Name == featureString, currentSection);
        if (featureSection == null)
        {
            featureSection = Section.CreateSection(featureString);
            currentSection.Sections.Add(featureSection);
        }
        var storySection = Section.FindSection(
            s => s.Name == storyString, featureSection);
        if (storySection == null)
        {
            storySection = Section.CreateSection(storyString);
            featureSection.Sections.Add(storySection);
        }
        testCase.SectionId = storySection.Id;

        return true;
    }


    protected virtual async Task<TestCase> ConvertTestCase(
        long testCaseId,
        Dictionary<string, Guid> sharedStepMap,
        Guid sectionId,
        Dictionary<string, Guid> attributes)
    {
        var testCase = await _client.GetTestCaseById(testCaseId);

        _logger.LogDebug("Found test case: {@TestCase}", testCase);

        var regularLinks = testCase.Links;
        var issueLinks = await _client.GetIssueLinks(testCaseId);
        // TODO: add relations somewhere (P: 1)
        // var relations = await _client.GetRelations(testCaseId);
        // var comments = await _client.GetComments(testCaseId);

        _logger.LogDebug("Found regular links: {@Links}", regularLinks);
        _logger.LogDebug("Found issue links: {@Links}", issueLinks);
        // _logger.LogDebug("Found relations: {@Links}", relations);
        // _logger.LogDebug("Found comments: {@Links}", comments);

        var tcIssueLinks = issueLinks.Select(l =>
            new Link { Url = l.Url, Title = l.Name, }).ToList();
        var tcLinks = regularLinks.Select(l =>
            new Link { Url = l.Url, Title = l.Name }).ToList();

        var testCaseGuid = Guid.NewGuid();
        var tmsAttachments = await _attachmentService.DownloadAttachmentsforTestCase(testCaseId, testCaseGuid);
        var preconditionSteps = testCase.Precondition != null ? [new Step { Action = testCase.Precondition }] : new List<Step>();
        var steps = await _stepService.ConvertStepsForTestCase(testCaseId, sharedStepMap);
        var caseAttributes = await ConvertAttributes(testCaseId, testCase, attributes);

        // max suite/story/feature length in allure 255 symbols already
        // TODO: add somewhere marker about cutting here
        var isNameCut = testCase.Name.Length > 255;
        if (isNameCut)
        {
            testCase.Name = "[CUT] " + CutToCharacters(testCase.Name, 255);
        }

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
            Links = tcLinks.Concat(tcIssueLinks).ToList(),
            Attributes = caseAttributes,
            Attachments = tmsAttachments,
            Steps = steps
        };

        _logger.LogDebug("Converted test case: {@TestCase}", allureTestCase);

        return allureTestCase;
    }

    public static string CutToCharacters(string input, int charCount)
    {
        if (string.IsNullOrEmpty(input))
            return input; // Return the input as-is if it's null or empty.

        return input.Length <= charCount ? input : input.Substring(0, charCount-9) + "...";
    }

    private async Task<List<CaseAttribute>> ConvertAttributes(long testCaseId, AllureTestCase testCase,
        Dictionary<string, Guid> attributes)
    {
        var caseAttributes = new List<CaseAttribute>
        {
            new CaseAttribute
            {
                Id = attributes[Constants.AllureStatus],
                Value = testCase.Status!.Name
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

            var customField = customFields.FirstOrDefault(
                cf => cf.CustomField!.Name == attribute.Key);

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
