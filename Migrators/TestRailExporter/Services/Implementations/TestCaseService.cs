using TestRailExporter.Client;
using Microsoft.Extensions.Logging;
using Models;
using TestRailExporter.Models.Client;
using TestRailExporter.Models.Commons;
using static System.Collections.Specialized.BitVector32;

namespace TestRailExporter.Services.Implementations;

public class TestCaseService(
    ILogger<TestCaseService> logger,
    IClient client,
    IAttachmentService attachmentService,
    IStepService stepService)
    : ITestCaseService
{
    public async Task<List<TestCase>> ConvertTestCases(int projectId, Dictionary<int, SharedStep> sharedStepMap,
        SectionInfo sectionInfo)
    {
        var sectionIdMap = sectionInfo.SectionsMap;
        var suiteIdMap = sectionInfo.SuitesMap;
        logger.LogInformation("Converting test cases");

        var allTestCases = new List<TestCase>();

        foreach (var section in sectionIdMap)
        {
            var testRailCases = new List<TestRailCase>();

            if (suiteIdMap.TryGetValue(section.Key, out int suiteId))
            {
                testRailCases = await client.GetTestCaseIdsByProjectIdAndSuiteIdAndSectionId(projectId, suiteId, section.Key);
            }
            else
            {
                testRailCases = await client.GetTestCaseIdsByProjectIdAndSectionId(projectId, section.Key);
            }

            var testCases = await ConvertTestCases(testRailCases, sharedStepMap, section.Value);

            allTestCases.AddRange(testCases);
        }

        logger.LogInformation("Ending converting test cases");

        return allTestCases;
    }

    private async Task<List<TestCase>> ConvertTestCases(
        List<TestRailCase> testRailCases,
        Dictionary<int, SharedStep> sharedStepMap,
        Guid sectionId)
    {
        var testCases = new List<TestCase>();

        foreach (var testRailCase in testRailCases)
        {
            var testCase = await ConvertTestCase(testRailCase, sharedStepMap, sectionId);
            testCases.Add(testCase);
        }

        return testCases;
    }

    protected async Task<TestCase> ConvertTestCase(
        TestRailCase testRailCase,
        Dictionary<int, SharedStep> sharedStepMap,
        Guid sectionId)
    {
        logger.LogDebug("Converting test case: {@Case}", testRailCase);

        var testCaseGuid = Guid.NewGuid();
        var attachmentsInfo = await attachmentService.DownloadAttachmentsByCaseId(testRailCase.Id, testCaseGuid);
        var preconditionSteps = testRailCase.TextPreconds != null ? [new Step { Action = testRailCase.TextPreconds }] : new List<Step>();
        var steps = await stepService.ConvertStepsForTestCase(testRailCase, testCaseGuid, sharedStepMap, attachmentsInfo.AttachmentsMap);

        // max suite/story/feature length in allure 255 symbols already
        // TODO: add somewhere marker about cutting here
        var isNameCut = testRailCase.Title.Length > 255;
        if (isNameCut)
        {
            testRailCase.Title = "[CUT] " + CutToCharacters(testRailCase.Title, 255);
        }

        var testCase = new TestCase
        {
            Id = testCaseGuid,
            Name = testRailCase.Title,
            State = StateType.NotReady,
            Priority = ConvertPriority(testRailCase.PriorityId),
            PreconditionSteps = preconditionSteps,
            PostconditionSteps = new List<Step>(),
            Tags = new List<string>(),
            Iterations = new List<Iteration>(),
            SectionId = sectionId,
            Attachments = attachmentsInfo.AttachmentNames,
            Steps = steps,
        };

        logger.LogDebug("Converted test case: {@TestCase}", testCase);

        return testCase;
    }

    public static string CutToCharacters(string input, int charCount)
    {
        if (string.IsNullOrEmpty(input))
            return input; // Return the input as-is if it's null or empty.

        return input.Length <= charCount ? input : input.Substring(0, charCount - 9) + "...";
    }

    private static PriorityType ConvertPriority(int priority)
    {
        return priority switch
        {
            1 => PriorityType.Low,
            2 => PriorityType.Medium,
            3 => PriorityType.High,
            4 => PriorityType.Highest,
            _ => PriorityType.Medium
        };
    }
}
