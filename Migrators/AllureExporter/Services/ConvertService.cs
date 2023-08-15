using AllureExporter.Client;
using AllureExporter.Models;
using Microsoft.Extensions.Logging;
using Models;

namespace AllureExporter.Services;

public class ConvertService
{
    private readonly ILogger<ConvertService> _logger;
    private readonly IClient _client;
    private readonly IWriteService _writeService;
    private readonly Dictionary<int, Guid> _sectionIdMap = new();

    public ConvertService(ILogger<ConvertService> logger, IClient client, IWriteService writeService)
    {
        _logger = logger;
        _client = client;
        _writeService = writeService;
    }

    public async Task ConvertSection()
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
                PreconditionSteps = Array.Empty<Step>(),
                PostconditionSteps = Array.Empty<Step>(),
                Sections = Array.Empty<Section>()
            };
            childSections.Add(childSection);
            _sectionIdMap.Add(s.Id, childSection.Id);
        }

        var section = new Section
        {
            Id = Guid.NewGuid(),
            Name = "Allure",
            PreconditionSteps = Array.Empty<Step>(),
            PostconditionSteps = Array.Empty<Step>(),
            Sections = childSections.ToArray()
        };
        _sectionIdMap.Add(0, section.Id);
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

    private async Task<Step[]> ConvertSteps(int testCaseId, Guid testCaseGuid)
    {
        var steps = await _client.GetSteps(testCaseId);

        var newSteps = new List<Step>();
        foreach (var allureStep in steps)
        {
            var newStep = await ConvertStep(testCaseGuid, allureStep);
            newSteps.Add(newStep);
        }

        return newSteps.ToArray();
    }

    private async Task<Step> ConvertStep(Guid testCaseId, AllureStep step)
    {
        var newStep = new Step
        {
            Action = step.Keyword + " " + step.Name
        };

        var newSteps = new List<Step>();
        foreach (var allureStep in step.Steps)
        {
            var newChildStep = await ConvertStep(testCaseId, allureStep);
            newSteps.Add(newChildStep);
        }

        newStep.Steps = newSteps.ToArray();
        newStep.Attachments = await DownloadAttachments(testCaseId, step.Attachments);

        return newStep;
    }

    private async Task<string[]> DownloadAttachments(Guid id, IEnumerable<AllureAttachment> attachments)
    {
        var names = new List<string>();

        foreach (var attachment in attachments)
        {
            var bytes = await _client.DownloadAttachment(attachment.Id);
            await _writeService.WriteAttachment(id, bytes, attachment.Name);
            names.Add(attachment.Name);
        }

        return names.ToArray();
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
            PreconditionSteps = Array.Empty<Step>(),
            PostconditionSteps = Array.Empty<Step>(),
            Tags = testCase.Tags.Select(t => t.Name).ToArray(),
            Iterations = Array.Empty<Iteration>(),
            SectionId = sectionId,
            Links = links.Select(l => new Link()
            {
                Url = l.Url,
                Title = l.Name,
            }).ToArray()
        };

        allureTestCase.Attachments = await DownloadAttachments(allureTestCase.Id, attachments);
        allureTestCase.Steps = await ConvertSteps(testCaseId, allureTestCase.Id);

        return allureTestCase;
    }
}
