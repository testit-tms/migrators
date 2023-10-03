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
    private Dictionary<string, Attribute> _attributeMap;

    public TestCaseService(ILogger<TestCaseService> logger, IClient client, IStepService stepService)
    {
        _logger = logger;
        _client = client;
        _stepService = stepService;
        _attributeMap = new Dictionary<string, Attribute>();
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
                if (_attributeMap.Count == 0 && zephyrTestCase.CustomFields.Count > 0)
                {
                    foreach (var keyValuePair in zephyrTestCase.CustomFields)
                    {
                        var attribute = new Attribute
                        {
                            Id = Guid.NewGuid(),
                            Name = keyValuePair.Key,
                            Type = AttributeType.String,
                            IsActive = true,
                            IsRequired = false,
                            Options = new List<string>()
                        };

                        _attributeMap.Add(keyValuePair.Key, attribute);
                    }
                }

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

                testCase.Attributes.AddRange(ConvertAttributes(zephyrTestCase.CustomFields));
                testCases.Add(testCase);
            }
        }

        return new TestCaseData
        {
            TestCases = testCases,
            Attributes = _attributeMap.Values.ToList()
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


    private List<CaseAttribute> ConvertAttributes(Dictionary<string, object> fields)
    {
        return fields
            .Select(field =>
                new CaseAttribute
                {
                    Id = _attributeMap[field.Key].Id,
                    Value = field.Value == null ? string.Empty : field.Value.ToString()
                }
            )
            .ToList();
    }
}
