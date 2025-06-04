using Microsoft.Extensions.Logging;
using Models;
using System.Text.RegularExpressions;
using TestLinkExporter.Client;
using TestLinkExporter.Models.TestCase;
using TestCase = Models.TestCase;
using Constants = TestLinkExporter.Models.Project.Constants;
using System.Collections.Generic;

namespace TestLinkExporter.Services.Implementations;

public class TestCaseService : ITestCaseService
{
    private readonly ILogger<TestCaseService> _logger;
    private readonly IClient _client;
    private readonly IStepService _stepService;
    private readonly IAttachmentService _attachmentService;
    public const int _duration = 10000;
    private static readonly Regex _hyperLinkRegex = new Regex(@"\<a\shref=\""(?<url>[^""\s]+)\""\>(?<title>.*?)\<\/a\>");

    public TestCaseService(ILogger<TestCaseService> logger, IClient client, IStepService stepService,
        IAttachmentService attachmentService)
    {
        _logger = logger;
        _client = client;
        _stepService = stepService;
        _attachmentService = attachmentService;
    }

    public async Task<List<TestCase>> ConvertTestCases(Dictionary<int, Guid> sectionMap, Dictionary<string, Guid> attributes)
    {
        _logger.LogInformation("Converting test cases");

        var testCases = new List<TestCase>();

        foreach (var section in sectionMap)
        {
            var testCaseIds = _client.GetTestCaseIdsBySuiteId(section.Key);

            _logger.LogDebug("Found {Count} test cases", testCaseIds.Count);

            foreach (var testCaseId in testCaseIds)
            {
                testCases.Add(
                    await ConvertTestCases(
                        _client.GetTestCaseById(testCaseId),
                        section.Value,
                        attributes
                    )
                );
            }
        }

        _logger.LogInformation("Exported test cases");

        return testCases;
    }

    private async Task<TestCase> ConvertTestCases(TestLinkTestCase testCase, Guid sectionId, Dictionary<string, Guid> attributes)
    {
        var testCaseId = Guid.NewGuid();
        var keywords = _client.GetKeywordsByTestCaseById(testCase.Id);
        var idAttribute = new CaseAttribute
        {
            Id = attributes[Constants.TestLinkId],
            Value = Constants.TestLinkPrefixId + testCase.ExternalId,
        };

        return new TestCase
        {
            Id = testCaseId,
            Description = testCase.Summary,
            State = StateType.NotReady,
            Priority = ConvertPriority(testCase.Importance),
            Steps = _stepService.ConvertSteps(testCase.Steps),
            PreconditionSteps = ConvertPreconditionSteps(testCase.Preconditions),
            PostconditionSteps = new List<Step>(),
            Duration = _duration,
            Attributes = [idAttribute],
            Tags = keywords,
            Attachments = await _attachmentService.DownloadAttachments(testCase.Id, testCaseId),
            Iterations = new List<Iteration>(),
            Links = GettingHyperlinks(testCase.Preconditions),
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
            _ => PriorityType.Medium
        };
    }

    private static List<Step> ConvertPreconditionSteps(string preconditions)
    {
        return new List<Step> {
            new Step
            {
                Action = Regex.Replace(preconditions, "<.*?>", string.Empty),
                Expected = string.Empty,
                TestData = string.Empty,
                ActionAttachments = new List<string>(),
                ExpectedAttachments = new List<string>(),
                TestDataAttachments = new List<string>()
            }
        };
    }

    public static List<Link> GettingHyperlinks(string description)
    {
        var matches = _hyperLinkRegex.Matches(description);
        var links = new List<Link>();

        if (matches.Count == 0)
        {
            return links;
        }

        foreach (Match match in matches)
        {
            var url = match.Groups["url"].Value;
            var title = match.Groups["title"].Value;

            links.Add(
                new Link
                {
                    Url = url,
                    Title = title,
                }
            );
        }

        return links;
    }
}
