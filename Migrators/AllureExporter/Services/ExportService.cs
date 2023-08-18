using System.Text;
using AllureExporter.Client;
using AllureExporter.Models;
using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;
using Attribute = Models.Attribute;

namespace AllureExporter.Services;

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly IClient _client;
    private readonly IWriteService _writeService;
    private readonly Dictionary<int, Guid> _sectionIdMap = new();
    private readonly Guid _attributeId = Guid.NewGuid();

    private const int MainSectionId = 0;

    public ExportService(ILogger<ExportService> logger, IClient client, IWriteService writeService)
    {
        _logger = logger;
        _client = client;
        _writeService = writeService;
    }

    public virtual async Task ExportProject()
    {
        _logger.LogInformation("Starting export");

        var project = await _client.GetProjectId();
        var section = await ConvertSection(project.Id);
        var testCaseGuids = await ConvertTestCase(project.Id);

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
                    Options = new List<string>
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

        _logger.LogInformation("Ending export");
    }

    protected virtual async Task<Section> ConvertSection(int projectId)
    {
        _logger.LogInformation("Converting sections");

        var sections = await _client.GetSuites(projectId);

        _logger.LogDebug("Found {Count} sections: {@Sections}", sections.Count, sections);

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
        _sectionIdMap.Add(MainSectionId, section.Id);

        _logger.LogDebug("Converted sections: {@Sections}", section);

        _logger.LogInformation("Ending converting sections");

        return section;
    }

    protected virtual async Task<List<Guid>> ConvertTestCase(int projectId)
    {
        _logger.LogInformation("Converting test cases");

        var testCaseGuids = new List<Guid>();
        foreach (var section in _sectionIdMap)
        {
            List<int> ids;
            if (section.Key == MainSectionId)
            {
                ids = await _client.GetTestCaseIdsFromMainSuite(projectId);
            }
            else
            {
                ids = await _client.GetTestCaseIdsFromSuite(projectId, section.Key);
            }

            foreach (var testCaseId in ids)
            {
                var testCase = await ConvertTestCase(testCaseId, section.Value);
                await _writeService.WriteTestCase(testCase);
                testCaseGuids.Add(testCase.Id);
            }
        }

        _logger.LogInformation("Ending converting test cases");

        return testCaseGuids;
    }

    protected virtual async Task<List<Step>> ConvertSteps(int testCaseId)
    {
        var steps = await _client.GetSteps(testCaseId);

        _logger.LogDebug("Found steps: {@Steps}", steps);

        return steps.Select(allureStep =>
            {
                var attachments = new List<string>();

                foreach (var allureStepStep in allureStep.Steps)
                {
                    attachments.AddRange(allureStepStep.Attachments.Select(a => a.Name));
                }

                var step = new Step
                {
                    Action = GetStepAction(allureStep),
                    Attachments = allureStep.Attachments.Select(a => a.Name).ToList()
                };

                step.Attachments.AddRange(attachments);

                return step;
            })
            .ToList();
    }

    private static string GetStepAction(AllureStep step)
    {
        var builder = new StringBuilder();

        if (!string.IsNullOrEmpty(step.Keyword))
        {
            builder.AppendLine($"<p>{step.Keyword}</p>");
        }

        builder.AppendLine($"<p>{step.Name}</p>");

        step.Steps
            .ForEach(s =>
            {
                if (!string.IsNullOrEmpty(s.Keyword))
                {
                    builder.AppendLine($"<p>{s.Keyword}</p>");
                }

                builder.AppendLine($"<p>{s.Name}</p>");
            });

        return builder.ToString();
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

    protected virtual async Task<TestCase> ConvertTestCase(int testCaseId, Guid sectionId)
    {
        var testCase = await _client.GetTestCaseById(testCaseId);

        _logger.LogDebug("Found test case: {@TestCase}", testCase);

        var attachments = await _client.GetAttachments(testCaseId);

        _logger.LogDebug("Found attachments: {@Attachments}", attachments);

        var links = await _client.GetLinks(testCaseId);

        _logger.LogDebug("Found links: {@Links}", links);

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
            Links = links.Select(l => new Link
            {
                Url = l.Url,
                Title = l.Name,
            }).ToList(),
            Attributes = new List<CaseAttribute>
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

        _logger.LogDebug("Converted test case: {@TestCase}", allureTestCase);

        return allureTestCase;
    }
}
