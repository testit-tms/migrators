using Microsoft.Extensions.Logging;
using Models;
using ZephyrScaleExporter.Client;
using ZephyrScaleExporter.Models;
using Attribute = Models.Attribute;
using Constants = ZephyrScaleExporter.Models.Constants;

namespace ZephyrScaleExporter.Services;

public class TestCaseService : ITestCaseService
{
    private readonly ILogger<TestCaseService> _logger;
    private readonly IClient _client;
    private readonly IStepService _stepService;

    public TestCaseService(ILogger<TestCaseService> logger, IClient client, IStepService stepService)
    {
        _logger = logger;
        _client = client;
        _stepService = stepService;
    }

    public async Task<TestCaseData> ConvertTestCases(Dictionary<int, Guid> sectionMap,
        Dictionary<string, Guid> attributeMap,
        Dictionary<int, string> statusMap, Dictionary<int, string> priorityMap)
    {
        _logger.LogInformation("Converting test cases");

        var testCases = new List<TestCase>();

        foreach (var section in sectionMap)
        {
            var cases = await _client.GetTestCases(section.Key);

            foreach (var zephyrTestCase in cases)
            {
                var steps = await _stepService.ConvertSteps(zephyrTestCase.Key, zephyrTestCase.TestScript.Self);

                var testCase = new TestCase
                {
                    Id = Guid.NewGuid(),
                    Description = zephyrTestCase.Description,
                    State = StateType.NotReady,
                    Priority = PriorityType.Medium,
                    Steps = steps,
                    PreconditionSteps = string.IsNullOrEmpty(zephyrTestCase.Precondition)
                        ? new List<Step>()
                        : new List<Step>
                        {
                            new()
                            {
                                Action = zephyrTestCase.Precondition,
                                Expected = string.Empty,
                                Attachments = new List<string>(),
                                TestData = string.Empty
                            }
                        },
                    PostconditionSteps = new List<Step>(),
                    Duration = 10,
                    Attributes = new List<CaseAttribute>
                    {
                        new()
                        {
                            Id = attributeMap[Constants.StateAttribute],
                            Value = statusMap[zephyrTestCase.Status.Id]
                        },
                        new()
                        {
                            Id = attributeMap[Constants.PriorityAttribute],
                            Value = priorityMap[zephyrTestCase.Priority.Id]
                        }
                    },
                    Tags = zephyrTestCase.Labels,
                    Attachments = new List<string>(),
                    Iterations = new List<Iteration>(),
                    Links = ConvertLinks(zephyrTestCase.Links),
                    Name = zephyrTestCase.Name,
                    SectionId = section.Value
                };

                testCases.Add(testCase);
            }
        }

        return new TestCaseData
        {
            TestCases = testCases,
            Attributes = new List<Attribute>()
        };
    }

    private static List<Link> ConvertLinks(Links links)
    {
        var newLinks = new List<Link>();

        newLinks.AddRange(
            links.WebLinks
                .Select(webLink =>
                    new Link
                    {
                        Title = webLink.Description,
                        Url = webLink.Url
                    }
                )
                .ToList()
        );

        newLinks.AddRange(
            links.Issues
                .Select(issue =>
                    new Link
                    {
                        Url = issue.Target
                    }
                )
                .ToList()
        );

        return newLinks;
    }
}
