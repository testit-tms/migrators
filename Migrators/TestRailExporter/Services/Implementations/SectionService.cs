using TestRailExporter.Client;
using Microsoft.Extensions.Logging;
using Models;
using TestRailExporter.Models.Client;
using TestRailExporter.Models.Commons;

namespace TestRailExporter.Services.Implementations;

public class SectionService(ILogger<SectionService> logger, IClient client) : ISectionService
{
    private readonly Dictionary<int, Guid> _sectionIdMap = new();
    private readonly Dictionary<int, int> _suiteIdMap = new();
    private const string _mainSectionName = "TestRail";

    public async Task<SectionInfo> ConvertSections(int projectId)
    {
        logger.LogInformation("Converting sections");

        var childSections = await ConvertSectionsWithSuites(projectId);

        var mainSection = new Section
        {
            Id = Guid.NewGuid(),
            Name = _mainSectionName,
            PreconditionSteps = new List<Step>(),
            PostconditionSteps = new List<Step>(),
            Sections = childSections,
        };

        logger.LogDebug("Converted sections: {@MainSection}", mainSection);

        logger.LogInformation("Ending converting sections");

        return new SectionInfo
        {
            MainSection = mainSection,
            SectionsMap = _sectionIdMap,
            SuitesMap = _suiteIdMap,
        };
    }

    private async Task<List<Section>> ConvertSectionsWithSuites(int projectId)
    {
        var testRailSuites = await client.GetSuitesByProjectId(projectId);

        if (testRailSuites.Count == 0)
        {
            var testRailSections = await client.GetSectionsByProjectId(projectId);

            return await ConvertSections(testRailSections, null);
        }

        var sections = new List<Section>();

        foreach (var testRailSuite in testRailSuites)
        {
            var testRailSections = await client.GetSectionsByProjectIdAndSuiteId(projectId, testRailSuite.Id);
            var childSections = await ConvertSections(testRailSections, null);

            var section = new Section
            {
                Id = Guid.NewGuid(),
                Name = testRailSuite.Name,
                PreconditionSteps = new List<Step>(),
                PostconditionSteps = new List<Step>(),
                Sections = childSections,
            };

            if (testRailSuite.Description != string.Empty)
            {
                section.PreconditionSteps.Add(new() { Action = testRailSuite.Description });
            }

            sections.Add(section);
        }

        return sections;
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

            if (testRailSection.Description != string.Empty)
            {
                section.PreconditionSteps.Add(new() { Action = testRailSection.Description });
            }

            sections.Add(section);
            _sectionIdMap.Add(testRailSection.Id, section.Id);

            if (testRailSection.SuiteId != null)
            {
                _suiteIdMap.Add(testRailSection.Id, testRailSection.SuiteId.Value);
            }
        }

        return sections;
    }
}
