using Microsoft.Extensions.Logging;
using Models;
using ZephyrSquadExporter.Client;
using ZephyrSquadExporter.Models;

namespace ZephyrSquadExporter.Services;

public class FolderService : IFolderService
{
    private readonly ILogger<FolderService> _logger;
    private readonly IClient _client;
    private readonly Dictionary<string, ZephyrSection> _sectionMap = new();


    public FolderService(ILogger<FolderService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<SectionData> GetSections()
    {
        _logger.LogInformation("Getting sections");

        var listOfFolders = new List<Section>();

        var cycles = await _client.GetCycles();

        foreach (var cycle in cycles)
        {
            var folders = await _client.GetFolders(cycle.Id);

            var cycleSection = new Section
            {
                Name = cycle.Name,
                Id = Guid.NewGuid(),
                PostconditionSteps = new List<Step>(),
                PreconditionSteps = new List<Step>(),
                Sections = new List<Section>()
            };

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

                cycleSection.Sections.Add(section);
                _sectionMap.Add(folder.Id, new ZephyrSection
                {
                    Id = section.Id,
                    IsFolder = true,
                    CycleId = cycle.Id
                });
            }

            _sectionMap.Add(cycle.Id, new ZephyrSection
            {
                Id = cycleSection.Id,
                IsFolder = false,
                CycleId = cycle.Id
            });

            listOfFolders.Add(cycleSection);
        }

        return new SectionData
        {
            Sections = listOfFolders,
            SectionMap = _sectionMap
        };
    }
}
