using Microsoft.Extensions.Logging;
using Models;
using System.Linq;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models;
using Attribute = Models.Attribute;
using Constants = ZephyrScaleServerExporter.Models.Constants;

namespace ZephyrScaleServerExporter.Services;

public class TestCaseService : ITestCaseService
{
    private readonly ILogger<TestCaseService> _logger;
    private readonly IClient _client;
    private readonly IStepService _stepService;
    private readonly IAttachmentService _attachmentService;
    public const int _duration = 10000;

    public TestCaseService(ILogger<TestCaseService> logger, IClient client, IStepService stepService,
        IAttachmentService attachmentService)
    {
        _logger = logger;
        _client = client;
        _stepService = stepService;
        _attachmentService = attachmentService;
    }

    public async Task<TestCaseData> ConvertTestCases(SectionData sectionData, Dictionary<string, Attribute> attributeMap)
    {
        _logger.LogInformation("Converting test cases");

        var testCases = new List<TestCase>();
        var requiredAttributeNames = attributeMap.Values.Where(a => a.IsRequired == true).Select(a => a.Name).ToList();

        var cases = await _client.GetTestCases();

        foreach (var zephyrTestCase in cases)
        {
            _logger.LogInformation("Converting test case {Name}", zephyrTestCase.Name);

            var attachments = new List<string>();

            var testCaseId = Guid.NewGuid();

            var zephyrAttacnhemts = await _client.GetAttachmentsForTestCase(zephyrTestCase.Key);

            foreach (var attachment in zephyrAttacnhemts)
            {
                var fileName = await _attachmentService.DownloadAttachment(testCaseId, attachment);
                attachments.Add(fileName);
            }

            var steps = zephyrTestCase.TestScript != null ?
                await _stepService.ConvertSteps(testCaseId, zephyrTestCase.TestScript) : new List<Step>();

            steps.ForEach(s =>
            {
                attachments.AddRange(s.ActionAttachments);
                attachments.AddRange(s.ExpectedAttachments);
                attachments.AddRange(s.TestDataAttachments);
            });

            var description = Utils.ExtractAttachments(zephyrTestCase.Description);

            foreach (var attachment in description.Attachments)
            {
                var fileName = await _attachmentService.DownloadAttachment(testCaseId, attachment);
                attachments.Add(fileName);
            }

            var precondition = Utils.ExtractAttachments(zephyrTestCase.Precondition);
            var preconditionAttachments = new List<string>();
            foreach (var attachment in precondition.Attachments)
            {
                var fileName = await _attachmentService.DownloadAttachment(testCaseId, attachment);
                preconditionAttachments.Add(fileName);
                attachments.Add(fileName);
            }

            var sectionId = ConvertFolders(zephyrTestCase.Folder, sectionData);
            var links = zephyrTestCase.IssueLinks != null ? await ConvertLinks(zephyrTestCase.IssueLinks) : new List<Link>();
            var attributes = new List<CaseAttribute>();

            if (!string.IsNullOrEmpty(zephyrTestCase.Component))
            {
                attributes.Add(
                    new()
                    {
                        Id = attributeMap[Constants.ComponentAttribute].Id,
                        Value = zephyrTestCase.Component
                    }
                );
            }

            attributes.Add(
                new()
                {
                    Id = attributeMap[Constants.IdZephyrAttribute].Id,
                    Value = zephyrTestCase.Key
                }
            );

            if (zephyrTestCase.CustomFields != null && zephyrTestCase.CustomFields.Count > 0)
            {
                attributes.AddRange(ConvertAttributes(zephyrTestCase.CustomFields, attributeMap));

                var UsedRequiredAttributeNames = zephyrTestCase.CustomFields.Keys.Where(n => requiredAttributeNames.Contains(n)).ToList();

                requiredAttributeNames.RemoveAll(n => UsedRequiredAttributeNames.Contains(n));

                attributeMap = CheckRequiredAttributes(attributeMap, requiredAttributeNames);

                requiredAttributeNames = UsedRequiredAttributeNames;
            }
            else
            {
                attributeMap = CheckRequiredAttributes(attributeMap, requiredAttributeNames);

                requiredAttributeNames.Clear();
            }

            var testCase = new TestCase
            {
                Id = testCaseId,
                Description = description.Description,
                State = ConvertStatus(zephyrTestCase.Status),
                Priority = ConvertPriority(zephyrTestCase.Priority),
                Steps = steps,
                PreconditionSteps = string.IsNullOrEmpty(zephyrTestCase.Precondition)
                    ? new List<Step>()
                    : new List<Step>
                    {
                        new()
                        {
                            Action = precondition.Description,
                            Expected = string.Empty,
                            ActionAttachments = preconditionAttachments,
                            TestData = string.Empty,
                            TestDataAttachments = new List<string>(),
                            ExpectedAttachments = new List<string>()
                        }
                    },
                PostconditionSteps = new List<Step>(),
                Duration = _duration,
                Attributes = attributes,
                Tags = zephyrTestCase.Labels ?? new List<string>(),
                Attachments = attachments,
                Iterations = new List<Iteration>(),
                Links = links,
                Name = zephyrTestCase.Name,
                SectionId = sectionId
            };

            testCases.Add(testCase);
        }

        return new TestCaseData
        {
            TestCases = testCases,
            Attributes = attributeMap.Values.ToList()
        };
    }

    private async Task<List<Link>> ConvertLinks(List<string> issueKeys)
    {
        _logger.LogInformation("Converting links for test case");

        var newLinks = new List<Link>();

        foreach (var issueKey in issueKeys)
        {
            var issue = await _client.GetIssueById(issueKey);

            newLinks.Add(
                new Link
                {
                    Title = issue.Fields.Name,
                    Url = issue.Url
                }
            );
        }

        return newLinks;
    }

    private Dictionary<string, Attribute> CheckRequiredAttributes(Dictionary<string, Attribute> attributeMap, List<string> unusedRequiredAttributeNames)
    {
        _logger.LogInformation("Checking required attributes");

        foreach (var unusedRequiredAttributeName in unusedRequiredAttributeNames)
        {
            _logger.LogInformation("Required attribute {Name} is not used. Set as optional", unusedRequiredAttributeName);

            var attribute = attributeMap[unusedRequiredAttributeName];

            attribute.IsRequired = false;

            attributeMap[unusedRequiredAttributeName] = attribute;
        }

        return attributeMap;
    }

    private List<CaseAttribute> ConvertAttributes(Dictionary<string, object> fields, Dictionary<string, Attribute> attributeMap)
    {
        _logger.LogInformation("Converting attributes for test case");
        var attributes = new List<CaseAttribute>();

        foreach (var field in fields)
        {
            _logger.LogInformation("Converting attribute \"{Key}\"", field.Key);

            if (!attributeMap.ContainsKey(field.Key))
            {
                _logger.LogInformation("The attribute \"{Key}\" cannot be obtained from the attribute map", field.Key);

                continue;
            }

            var attribute = attributeMap[field.Key];
            var zephyrValue = field.Value == null ? string.Empty : field.Value.ToString();

            attributes.Add(
                new CaseAttribute
                {
                    Id = attribute.Id,
                    Value = attribute.Type == AttributeType.MultipleOptions ? ConvertMultipleValue(zephyrValue, attribute.Options) : zephyrValue,
                }
            );
        }

        _logger.LogInformation("Converted attributes {@Attributes}", attributes);

        return attributes;
    }

    private List<string> ConvertMultipleValue(string attributeValue, List<string> options)
    {
        _logger.LogInformation("Converting multiple value {Value} with options {@Options}", attributeValue, options);

        var testCaseValues = new List<string>();

        foreach (var option in options)
        {
            if (attributeValue.Contains(option) && (attributeValue.Contains(option + ", ") || attributeValue.Contains(", " + option) || attributeValue == option))
            {
                testCaseValues.Add(option);

                _logger.LogInformation("The option \"{Option}\" add to multiple choice for test case", option);
            }
        }

        _logger.LogInformation("Converted multiple value {Value} to options {@Options}", attributeValue, testCaseValues);

        return testCaseValues;
    }

    private Guid ConvertFolders(string stringFolders, SectionData sectionData)
    {
        _logger.LogInformation("Converting folders");

        var sectionKey = Constants.MainFolderKey;

        if (stringFolders == null)
        {
            return sectionData.SectionMap[sectionKey];
        }

        if (sectionData.AllSections.ContainsKey(sectionKey + stringFolders))
        {
            return sectionData.SectionMap[sectionKey + stringFolders];
        }

        var lastSectionId = sectionData.SectionMap[sectionKey];
        var folders = stringFolders.Split('/');

        foreach (var folder in folders)
        {
            if (string.IsNullOrEmpty(folder))
            {
                continue;
            }

            if (!sectionData.AllSections.ContainsKey(sectionKey + "/" + folder))
            {
                var section = new Section
                {
                    Id = Guid.NewGuid(),
                    Name = folder,
                    Sections = new List<Section>(),
                    PostconditionSteps = new List<Step>(),
                    PreconditionSteps = new List<Step>()
                };

                if (!sectionData.AllSections.ContainsKey(sectionKey))
                {
                    _logger.LogInformation("The section \"{Key}\" cannot be obtained from the all sections map", sectionKey);

                    continue;
                }

                sectionData.AllSections[sectionKey].Sections.Add(section);
                sectionData.SectionMap.Add(sectionKey + "/" + folder, section.Id);
                sectionData.AllSections.Add(sectionKey + "/" + folder, section);
            }

            sectionKey += "/" + folder;
            lastSectionId = sectionData.SectionMap[sectionKey];
        }

        return lastSectionId;
    }

    private static PriorityType ConvertPriority(string priority)
    {
        return priority switch
        {
            "High" => PriorityType.High,
            "Normal" => PriorityType.Medium,
            "Low" => PriorityType.Low,
            _ => PriorityType.Medium
        };
    }

    private static StateType ConvertStatus(string status)
    {
        return status switch
        {
            "Approved" => StateType.Ready,
            "Draft" => StateType.NotReady,
            "Deprecated" => StateType.NeedsWork,
            _ => StateType.NotReady
        };
    }
}
