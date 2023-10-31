using Microsoft.Extensions.Logging;
using Models;
using System;
using System.Text.RegularExpressions;
using TestLinkExporter.Client;
using TestLinkExporter.Models;

namespace TestLinkExporter.Services;

public class TestCaseService : ITestCaseService
{
    private readonly ILogger<TestCaseService> _logger;
    private readonly IClient _client;
    private readonly IStepService _stepService;
    private readonly IAttachmentService _attachmentService;

    public TestCaseService(ILogger<TestCaseService> logger, IClient client, IStepService stepService,
        IAttachmentService attachmentService)
    {
        _logger = logger;
        _client = client;
        _stepService = stepService;
        _attachmentService = attachmentService;
    }

    public List<TestCase> ConvertTestCases(Dictionary<int, Guid> sectionMap)
    {
        _logger.LogInformation("Converting test cases");

        var testCases = new List<TestCase>();

        foreach (var section in sectionMap)
        {
            var testCaseIds = _client.GetTestCaseIdsBySuiteId(section.Key);

            _logger.LogDebug("Found {@TestCaseIds} test cases", testCaseIds.Count);

            foreach (var testCaseId in testCaseIds)
            {
                testCases.Add(ConvertTestCases(
                    _client.GetTestCaseById(testCaseId),
                    section.Value));
            }
        }

        _logger.LogInformation("Exported test cases");

        return testCases;
    }

    private TestCase ConvertTestCases(TestLinkTestCase testCase, Guid sectionId)
    {
        var testCaseId = Guid.NewGuid();

        return new TestCase
        {
            Id = testCaseId,
            Description = testCase.Summary,
            State = StateType.NotReady,
            Priority = ConvertPriority(testCase.Importance),
            Steps = _stepService.ConvertSteps(testCase.Steps),
            PreconditionSteps = ConvertPreconditionSteps(testCase.Preconditions),
            PostconditionSteps = new List<Step>(),
            Duration = 10,
            Attributes = new List<CaseAttribute>(),
            Tags = new List<string>(),
            Attachments = _attachmentService.DownloadAttachments(testCase.Id, testCaseId),
            Iterations = new List<Iteration>(),
            Links = new List<Link>(),
            Name = testCase.Name,
            SectionId = sectionId
        };
    }

    private static PriorityType ConvertPriority(int priority)
    {
        return priority switch
        {
            3 => PriorityType.High,
            2 => PriorityType.Medium,
            1 => PriorityType.Low,
            _ => throw new Exception($"Failed to convert priority {priority}")
        };
    }

    private static List<Step> ConvertPreconditionSteps(string preconditions)
    {
        return new List<Step> {
            new Step
            {
                Action = Regex.Replace(preconditions, "<.*?>", String.Empty),
                Expected = string.Empty,
                TestData = string.Empty,
                ActionAttachments = new List<string>(),
                ExpectedAttachments = new List<string>(),
                TestDataAttachments = new List<string>()
            }
        };
    }
}
