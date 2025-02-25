using TestRailExporter.Client;
using TestRailExporter.Models;
using Microsoft.Extensions.Logging;
using Models;

namespace TestRailExporter.Services;

public class SectionService : ISectionService
{
    private readonly ILogger<SectionService> _logger;
    private readonly IClient _client;
    private readonly Dictionary<int, Guid> _sectionIdMap;
    private const string _mainSectionName = "TestRail";

    public SectionService(ILogger<SectionService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
        _sectionIdMap = new Dictionary<int, Guid>();
    }

    public async Task<SectionInfo> ConvertSections(int projectId)
    {
        _logger.LogInformation("Converting sections");

        var testRailSections = await _client.GetSectionsByProjectId(projectId);

        _logger.LogDebug("Found {Count} sections: {@Sections}", testRailSections.Count, testRailSections);

        var childSections = await ConvertSections(testRailSections, null);

        var mainSection = new Section
        {
            Id = Guid.NewGuid(),
            Name = _mainSectionName,
            PreconditionSteps = new List<Step>(),
            PostconditionSteps = new List<Step>(),
            Sections = childSections,
        };

        _logger.LogDebug("Converted sections: {@MainSection}", mainSection);

        _logger.LogInformation("Ending converting sections");

        return new SectionInfo
        {
            MainSection = mainSection,
            SectionsMap = _sectionIdMap
        };
    }

    private async Task<List<Section>> ConvertSections(List<TestRailSection> testRailSections, int? parentId)
    {
        var sections = new List<Section>();
        var mainSections = testRailSections.FindAll(s => s.ParentId == parentId);

        foreach (var testRailSection in mainSections)
        {
            var childSections = await ConvertSections(testRailSections, testRailSection.Id);

            var section = new Section
            {
                Id = Guid.NewGuid(),
                Name = testRailSection.Name,
                PreconditionSteps = new List<Step>(),
                PostconditionSteps = new List<Step>(),
                Sections = childSections,
            };

            if (testRailSection.Description != String.Empty)
            {
                section.PreconditionSteps.Add(new() { Action = testRailSection.Description });
            }

            sections.Add(section);
            _sectionIdMap.Add(testRailSection.Id, section.Id);
        }

        return sections;
    }
}
