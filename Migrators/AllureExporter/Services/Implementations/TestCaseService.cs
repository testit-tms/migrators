using AllureExporter.Client;
using AllureExporter.Models.Config;
using AllureExporter.Models.Project;
using AllureExporter.Models.TestCase;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Constants = AllureExporter.Models.Project.Constants;

namespace AllureExporter.Services.Implementations;

internal sealed class TestCaseService(
    ILogger<TestCaseService> logger,
    IClient client,
    IAttachmentService attachmentService,
    IStepService stepService,
    IOptions<AppConfig> config)
    : ITestCaseService
{
    private const int MaxAllowedTestNameLength = 255;
    private const bool CutTestCaseNameEnabled = true;

    public async Task<List<TestCase>> ConvertTestCases(
        long projectId,
        Dictionary<string, Guid> sharedStepMap,
        Dictionary<string, Guid> attributes,
        SectionInfo sectionInfo)
    {
        var sectionIdMap = sectionInfo.SectionDictionary;
        logger.LogInformation("Converting test cases");

        var testCases = new List<TestCase>();
        foreach (var section in sectionIdMap)
        {
            List<long> ids;
            if (section.Key == Constants.MainSectionId)
                ids = await client.GetTestCaseIdsFromMainSuite(projectId);
            else
                ids = await client.GetTestCaseIdsFromSuite(projectId, section.Key);

            foreach (var testCaseId in ids)
            {
                var testCase = await ConvertTestCase(projectId, testCaseId, sharedStepMap, section.Value, attributes);
                ProcessFeatureSection(testCase, attributes, sectionInfo);
                testCases.Add(testCase);
            }
        }

        logger.LogInformation("Ending converting test cases");

        return testCases;
    }

    private string GetAttributeNameById(Guid id, List<CaseAttribute> caseAttributes)
    {
        var attr = caseAttributes.FirstOrDefault(x =>
            x.Id == id);
        return attr?.Value.ToString() ?? string.Empty;
    }

    /// <summary>
    ///     create feauture and story sections, attach testCase to the story;
    /// </summary>
    /// <returns>true if processed successfully</returns>
    private bool ProcessFeatureSection(TestCase testCase,
        Dictionary<string, Guid> attributes,
        SectionInfo sectionInfo)
    {
        var featureString = GetAttributeNameById(attributes[Constants.Feature], testCase.Attributes);
        var storyString = GetAttributeNameById(attributes[Constants.Story], testCase.Attributes);
        if (featureString == "" || storyString == "") return false;

        var currentSection = Section.FindSection(s => s.Id == testCase.SectionId, sectionInfo.MainSection)!;

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


    private async Task<TestCase> ConvertTestCase(
        long projectId,
        long testCaseId,
        Dictionary<string, Guid> sharedStepMap,
        Guid sectionId,
        Dictionary<string, Guid> attributes)
    {
        var testCase = await client.GetTestCaseById(testCaseId);

        logger.LogDebug("Found test case: {@TestCase}", testCase);

        var regularLinks = testCase.Links;
        var issueLinks = await client.GetIssueLinks(testCaseId);
        // TODO: add relations somewhere (P: 1)
        var relations = await client.GetRelations(testCaseId);
        // var comments = await _client.GetComments(testCaseId);

        logger.LogDebug("Found regular links: {@Links}", regularLinks);
        logger.LogDebug("Found issue links: {@Links}", issueLinks);
        // _logger.LogDebug("Found relations: {@Links}", relations);
        // _logger.LogDebug("Found comments: {@Links}", comments);

        var tcIssueLinks = issueLinks.Select(l =>
            new Link { Url = l.Url, Title = l.Name }).ToList();
        var tcLinks = regularLinks.Select(l =>
            new Link { Url = l.Url, Title = l.Name }).ToList();

        var relationTestCaseMask = $"{config.Value.Allure.Url}/project/{projectId}/test-cases";

        var relationsAsLinks = relations.Select(l =>
            new Link
            {
                Url = $"{relationTestCaseMask}/{l.Target.Id}",
                Title = $"Type: {l.Type}, Name: {l.Target.Name}"
            }).ToList();

        var links = tcLinks
            .Concat(tcIssueLinks)
            .Concat(relationsAsLinks)
            .ToList();

        var testCaseGuid = Guid.NewGuid();
        var tmsAttachments = await attachmentService.DownloadAttachmentsforTestCase(testCaseId, testCaseGuid);
        var preconditionSteps = testCase.PreconditionHtml != null
            ? [new Step { Action = testCase.PreconditionHtml }]
            : new List<Step>();

        var postconditionSteps = testCase.ExpectedResultHtml != null
            ? [new Step { Action = testCase.ExpectedResultHtml }]
            : new List<Step>();

        var steps = await stepService.ConvertStepsForTestCase(testCaseId, sharedStepMap);
        var caseAttributes = await ConvertAttributes(testCaseId, testCase, attributes);

        // max suite/story/feature length in allure 255 symbols already
        CutNameFeature(testCase);

        var allureTestCase = new TestCase
        {
            Id = testCaseGuid,
            Name = testCase.Name,
            Description = testCase.Description,
            State = StateType.NotReady,
            Priority = PriorityType.Medium,
            PreconditionSteps = preconditionSteps,
            PostconditionSteps = postconditionSteps,
            Tags = testCase.Tags.Select(t => t.Name).ToList(),
            Iterations = new List<Iteration>(),
            SectionId = sectionId,
            Links = links,
            Attributes = caseAttributes,
            Attachments = tmsAttachments,
            Steps = steps
        };

        logger.LogDebug("Converted test case: {@TestCase}", allureTestCase);

        return allureTestCase;
    }

    // "OldNameXXX" -> "[CUT] OldNameXX..."
    private static void CutNameFeature(AllureTestCase testCase)
    {
        if (!CutTestCaseNameEnabled) return;
        // max suite/story/feature length in allure 255 symbols already
        // TODO: add somewhere marker about cutting here
        if (testCase.Name.Length <= MaxAllowedTestNameLength) return;
        const string mask = "[CUT] ";
        //
        testCase.Name = mask + CutToCharacters(testCase.Name, MaxAllowedTestNameLength - mask.Length);
    }

    private static string CutToCharacters(string input, int charCount)
    {
        if (string.IsNullOrEmpty(input))
            return input; // Return the input as-is if it's null or empty.

        return input.Length <= charCount ? input : input.Substring(0, charCount - 3) + "...";
    }

    private async Task<List<CaseAttribute>> ConvertAttributes(long testCaseId, AllureTestCase testCase,
        Dictionary<string, Guid> attributes)
    {
        var caseAttributes = new List<CaseAttribute>
        {
            new()
            {
                Id = attributes[Constants.AllureStatus],
                Value = testCase.Status!.Name
            },
                        new()
            {
                Id = attributes[Constants.AllureId],
                Value = testCase.Id.ToString()
            },
            new()
            {
                Id = attributes[Constants.AllureTestLayer],
                Value = testCase.Layer != null ? testCase.Layer.Name : string.Empty
            }
        };

        var customFields = await client.GetCustomFieldsFromTestCase(testCaseId);

        foreach (var attribute in attributes)
        {
            if (attribute.Key is Constants.AllureStatus or Constants.AllureTestLayer) continue;

            var customField = customFields.FirstOrDefault(
                cf => cf.CustomField!.Name == attribute.Key);

            if (customField != null)
                caseAttributes.Add(new CaseAttribute
                {
                    Id = attribute.Value,
                    Value = customField.Name
                });
            else
                caseAttributes.Add(new CaseAttribute
                {
                    Id = attribute.Value,
                    Value = string.Empty
                });
        }

        return caseAttributes;
    }
}
