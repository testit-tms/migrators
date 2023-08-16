using AllureExporter.Client;
using AllureExporter.Models;
using Microsoft.Extensions.Logging;
using Models;
using Attribute = Models.Attribute;

namespace AllureExporter.Services;

public class ConvertService
{
    private readonly ILogger<ConvertService> _logger;
    private readonly IClient _client;
    private readonly IWriteService _writeService;
    private readonly Dictionary<int, Guid> _sectionIdMap = new();
    private readonly Guid _attributeId = Guid.NewGuid();

    public ConvertService(ILogger<ConvertService> logger, IClient client, IWriteService writeService)
    {
        _logger = logger;
        _client = client;
        _writeService = writeService;
    }

    public async Task ConvertMainJson()
    {
        var project = await _client.GetProjectId();
        var section = await ConvertSection();
        var testCaseGuids = await ConvertTestCase();

        var mainJson = new Root
        {
            ProjectName = project.Name,
            Sections = new List<Section> { section },
            TestCases = testCaseGuids,
            SharedSteps = new List<Guid>(),
            Attributes = new List<Attribute>
            {
                new()
                {
                    Id = _attributeId,
                    Name = "AllureStatus",
                    IsActive = true,
                    IsRequired = true,
                    Type = AttributeType.Options,
                    Options = new List<string>()
                    {
                        "Draft",
                        "Active",
                        "Outdated",
                        "Review"
                    }
                }
            }
        };

        await _writeService.WriteMainJson(mainJson);
    }

    public async Task<Section> ConvertSection()
    {
        var project = await _client.GetProjectId();
        var sections = await _client.GetSuites(project.Id);

        var childSections = new List<Section>();

        foreach (var s in sections)
        {
            var childSection = new Section
            {
                Id = Guid.NewGuid(),
                Name = s.Name,
                PreconditionSteps = new List<Step>(),
                PostconditionSteps = new List<Step>(),
                Sections = new List<Section>()
            };
            childSections.Add(childSection);
            _sectionIdMap.Add(s.Id, childSection.Id);
        }

        var section = new Section
        {
            Id = Guid.NewGuid(),
            Name = "Allure",
            PreconditionSteps = new List<Step>(),
            PostconditionSteps = new List<Step>(),
            Sections = childSections
        };
        _sectionIdMap.Add(0, section.Id);

        return section;
    }

    public async Task<List<Guid>> ConvertTestCase()
    {
        var project = await _client.GetProjectId();

        var testCaseGuids = new List<Guid>();
        foreach (var section in _sectionIdMap)
        {
            List<int> ids;
            if (section.Key == 0)
            {
                ids = await _client.GetTestCaseIdsFromMainSuite(project.Id);
            }
            else
            {
                ids = await _client.GetTestCaseIdsFromSuite(project.Id, section.Key);
            }

            foreach (var testCaseId in ids)
            {
                var testCase = await ConvertTestCase(testCaseId, section.Value);
                await _writeService.WriteTestCase(testCase);
                testCaseGuids.Add(testCase.Id);
            }
        }

        return testCaseGuids;
    }

    private async Task<List<Step>> ConvertSteps(int testCaseId)
    {
        var steps = await _client.GetSteps(testCaseId);

        return steps.Select(allureStep =>
            {
                var childSteps = allureStep.Steps
                    .Select(s => s.Keyword + "\n" + s.Name)
                    .ToList();

                var attachments = new List<string>();

                foreach (var allureStepStep in allureStep.Steps)
                {
                    attachments.AddRange(allureStepStep.Attachments.Select(a => a.Name));
                }

                var step = new Step
                {
                    Action = allureStep.Keyword + "\n" + allureStep.Name + "\n" + string.Join("\n", childSteps),
                    Attachments = allureStep.Attachments.Select(a => a.Name).ToList()
                };

                step.Attachments.AddRange(attachments);

                return step;
            })
            .ToList();
    }

    private async Task<List<string>> DownloadAttachments(Guid id, IEnumerable<AllureAttachment> attachments)
    {
        var names = new List<string>();

        foreach (var attachment in attachments)
        {
            var bytes = await _client.DownloadAttachment(attachment.Id);
            await _writeService.WriteAttachment(id, bytes, attachment.Name);
            names.Add(attachment.Name);
        }

        return names;
    }

    private async Task<TestCase> ConvertTestCase(int testCaseId, Guid sectionId)
    {
        var testCase = await _client.GetTestCaseById(testCaseId);

        var attachments = await _client.GetAttachments(testCaseId);
        var links = await _client.GetLinks(testCaseId);

        var allureTestCase = new TestCase
        {
            Id = Guid.NewGuid(),
            Name = testCase.Name,
            Description = testCase.Description,
            State = StateType.NotReady,
            Priority = PriorityType.Medium,
            PreconditionSteps = new List<Step>(),
            PostconditionSteps = new List<Step>(),
            Tags = testCase.Tags.Select(t => t.Name).ToList(),
            Iterations = new List<Iteration>(),
            SectionId = sectionId,
            Links = links.Select(l => new Link()
            {
                Url = l.Url,
                Title = l.Name,
            }).ToList(),
            Attributes = new List<CaseAttribute>()
            {
                new()
                {
                    Id = _attributeId,
                    Value = testCase.Status.Name
                }
            }
        };

        allureTestCase.Attachments = await DownloadAttachments(allureTestCase.Id, attachments);
        allureTestCase.Steps = await ConvertSteps(testCaseId);

        return allureTestCase;
    }
}
