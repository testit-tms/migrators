using Microsoft.Extensions.Logging;
using Models;
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
    private readonly Dictionary<string, Attribute> _attributeMap;
    public const int _duration = 10000;

    public TestCaseService(ILogger<TestCaseService> logger, IClient client, IStepService stepService,
        IAttachmentService attachmentService)
    {
        _logger = logger;
        _client = client;
        _stepService = stepService;
        _attachmentService = attachmentService;
        _attributeMap = new Dictionary<string, Attribute>();
    }

    public async Task<TestCaseData> ConvertTestCases(SectionData sectionData, Dictionary<string, Guid> attributeMap)
    {
        _logger.LogInformation("Converting test cases");

        var testCases = new List<TestCase>();

        var cases = await _client.GetTestCases();

        foreach (var zephyrTestCase in cases)
        {
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
                        Id = attributeMap[Constants.ComponentAttribute],
                        Value = zephyrTestCase.Component
                    }
                );
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

            if (_attributeMap.Count == 0 && zephyrTestCase.CustomFields != null && zephyrTestCase.CustomFields.Count > 0)
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
                    testCase.Attributes.AddRange(ConvertAttributes(zephyrTestCase.CustomFields));
                }
            }
            testCases.Add(testCase);
        }

        return new TestCaseData
        {
            TestCases = testCases,
            Attributes = _attributeMap.Values.ToList()
        };
    }

    private async Task<List<Link>> ConvertLinks(List<string> issueKeys)
    {
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

    private static Guid ConvertFolders(string stringFolders, SectionData sectionData)
    {
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
