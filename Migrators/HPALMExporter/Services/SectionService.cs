using HPALMExporter.Client;
using HPALMExporter.Models;
using Microsoft.Extensions.Logging;
using Models;

namespace HPALMExporter.Services;

public class SectionService : ISectionService
{
    private readonly ILogger<SectionService> _logger;
    private readonly IClient _client;
    private readonly Dictionary<int, Guid> _sectionMap;

    private const int RootFolderId = 0;
    private const int UnattachedFolderId = -2;

    public SectionService(ILogger<SectionService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
        _sectionMap = new Dictionary<int, Guid>();
    }

    public async Task<SectionData> ConvertSections()
    {
        _logger.LogInformation("Get sections from HP ALM");

        var sections = await ConvertSubSection(RootFolderId);
        var subjectSection = sections.FirstOrDefault();

        if (subjectSection != null)
        {
            var unattachedSection = new Section
            {
                Id = Guid.NewGuid(),
                Name = "Unattached",
                PreconditionSteps = new List<Step>(),
                PostconditionSteps = new List<Step>(),
                Sections = new List<Section>()
            };

            _sectionMap.Add(UnattachedFolderId, unattachedSection.Id);
            subjectSection.Sections.Add(unattachedSection);
        }

        return new SectionData
        {
            Sections = sections,
            SectionMap = _sectionMap
        };
    }


    private async Task<List<Section>> ConvertSubSection(int parentId)
    {
        _logger.LogDebug("Convert subsections from HP ALM {ParentId}", parentId);

        var folders = await _client.GetTestFolders(parentId);
        var sections = new List<Section>(folders.Count);

        foreach (var folder in folders)
        {
            var subSections = await ConvertSubSection(folder.Id);

            var section = new Section
            {
                Id = Guid.NewGuid(),
                Name = folder.Name,
                PreconditionSteps = new List<Step>(),
                PostconditionSteps = new List<Step>(),
                Sections = subSections
            };

            sections.Add(section);
            _sectionMap.Add(folder.Id, section.Id);
        }

        return sections;
    }
}
