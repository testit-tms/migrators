using Microsoft.Extensions.Logging;
using Models;
using System;
using ZephyrSquadServerExporter.Client;
using ZephyrSquadServerExporter.Models;

namespace ZephyrSquadServerExporter.Services;

public class FolderService : IFolderService
{
    private readonly ILogger<FolderService> _logger;
    private readonly IClient _client;
    private readonly List<ZephyrSection> _allSections = new();


    public FolderService(ILogger<FolderService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<SectionData> GetSections(List<JiraProjectVersion> versions, string projectId)
    {
        _logger.LogInformation("Getting sections");

        var listOfSections = new List<Section>();

        versions.Insert(0, new JiraProjectVersion
        {
            Name = "Unscheduled",
            Id = "-1",
            ProjectId = int.Parse(projectId),
        });

        foreach (var version in versions)
        {
            var listOfCycles = await GetCycles(version);

            var section = new Section
            {
                Name = version.Name,
                Id = Guid.NewGuid(),
                PostconditionSteps = new List<Step>(),
                PreconditionSteps = new List<Step>(),
                Sections = listOfCycles
            };

            listOfSections.Add(section);
        }

        return new SectionData
        {
            SectionsTree = listOfSections,
            AllSections = _allSections
        };
    }

    private async Task<List<Section>> GetCycles(JiraProjectVersion version)
    {
        var listOfCycles = new List<Section>();
        var cycles = await _client.GetCyclesByProjectIdAndVersionId(version.ProjectId.ToString(), version.Id);

        foreach (var cycle in cycles)
        {
            var listOfFolders = await GetFolders(cycle);

            var cycleSection = new Section
            {
                Name = cycle.Name,
                Id = Guid.NewGuid(),
                PostconditionSteps = new List<Step>(),
                PreconditionSteps = new List<Step>(),
                Sections = listOfFolders
            };

            _allSections.Add(new ZephyrSection
            {
                Id = cycleSection.Id,
                ProjectId = version.ProjectId.ToString(),
                VersionId = version.Id,
                CycleId = cycle.Id
            });

            listOfCycles.Add(cycleSection);
        }

        return listOfCycles;
    }

    private async Task<List<Section>> GetFolders(ZephyrCycle cycle)
    {
        var listOfFolders = new List<Section>();

        if (!cycle.Id.Equals("-1"))
        {
            var folders = await _client.GetFoldersByProjectIdAndVersionIdAndCycleId(cycle.ProjectId.ToString(), cycle.VersionId.ToString(), cycle.Id);

            foreach (var folder in folders)
            {
                var section = new Section
                {
                    Name = folder.Name,
                    Id = Guid.NewGuid(),
                    PostconditionSteps = new List<Step>(),
                    PreconditionSteps = new List<Step>(),
                    Sections = new List<Section>()
                };

                _allSections.Add(new ZephyrSection
                {
                    Id = section.Id,
                    ProjectId = cycle.ProjectId.ToString(),
                    VersionId = cycle.VersionId.ToString(),
                    CycleId = cycle.Id,
                    FolderId = folder.Id.ToString()
                });

                listOfFolders.Add(section);
            }
        }

        return listOfFolders;
    }
}
