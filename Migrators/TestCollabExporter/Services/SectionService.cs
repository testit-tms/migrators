using Microsoft.Extensions.Logging;
using Models;
using TestCollabExporter.Client;
using TestCollabExporter.Models;

namespace TestCollabExporter.Services;

public class SectionService : ISectionService
{
    private readonly ILogger<SectionService> _logger;
    private readonly IClient _client;
    private readonly Dictionary<int, Guid> _sectionMap;

    public SectionService(ILogger<SectionService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
        _sectionMap = new Dictionary<int, Guid>();
    }

    public async Task<SectionData> ConvertSection(int projectId)
    {
        _logger.LogInformation("Getting sections");

        var suites = await _client.GetSuites(projectId);
        var sharedStepsSection = GetSharedStepsSection();
        var sections = new List<Section>();

        foreach (var suite in suites.Where(s => s.Parent_id == 0))
        {
            var section = new Section
            {
                Id = Guid.NewGuid(),
                Name = suite.Title,
                PreconditionSteps = new List<Step>(),
                PostconditionSteps = new List<Step>(),
                Sections = ConvertChildSection(suite.Id, suites)
            };

            sections.Add(section);
            _sectionMap.Add(suite.Id, section.Id);
        }

        return new SectionData
        {
            Sections = sections,
            SectionMap = _sectionMap,
            SharedStepSection = sharedStepsSection
        };
    }

    private Section GetSharedStepsSection()
    {
        return new Section
        {
            Id = Guid.NewGuid(),
            Name = "Shared Steps",
            PostconditionSteps = new List<Step>(),
            PreconditionSteps = new List<Step>(),
            Sections = new List<Section>()
        };
    }

    private List<Section> ConvertChildSection(int parentId, IEnumerable<TestCollabSuite> suites)
    {
        var sections = new List<Section>();
        var testCollabSuites = suites.ToList();
        var childSuites = testCollabSuites.Where(s => s.Parent_id == parentId);

        foreach (var childSuite in childSuites)
        {
            var section = new Section
            {
                Id = Guid.NewGuid(),
                Name = childSuite.Title,
                PreconditionSteps = new List<Step>(),
                PostconditionSteps = new List<Step>(),
                Sections = ConvertChildSection(childSuite.Id, testCollabSuites)
            };

            sections.Add(section);
            _sectionMap.Add(childSuite.Id, section.Id);
        }

        return sections;
    }
}
