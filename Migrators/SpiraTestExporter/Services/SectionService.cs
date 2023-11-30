using Microsoft.Extensions.Logging;
using Models;
using SpiraTestExporter.Client;
using SpiraTestExporter.Models;

namespace SpiraTestExporter.Services;

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

    public async Task<SectionData> GetSections(int projectId)
    {
        _logger.LogInformation("Getting sections for project {ProjectId}", projectId);

        var sections = new List<Section>();

        var folders = await _client.GetFolders(projectId);

        foreach (var folder in folders.Where(f => f.ParentId == null))
        {
            var section = new Section
            {
                Id = Guid.NewGuid(),
                Name = folder.Name,
                PreconditionSteps = new List<Step>(),
                PostconditionSteps = new List<Step>(),
                Sections = ConvertChildSection(folder.Id, folders)
            };

            sections.Add(section);
            _sectionMap.Add(folder.Id, section.Id);
        }

        return new SectionData
        {
            Sections = sections,
            SectionMap = _sectionMap
        };
    }

    private List<Section> ConvertChildSection(int parentId, IEnumerable<SpiraFolder> folders)
    {
        var sections = new List<Section>();
        var spiraFolders = folders.ToList();
        var childFolders = spiraFolders.Where(s => s.ParentId == parentId);

        foreach (var child in childFolders)
        {
            var section = new Section
            {
                Id = Guid.NewGuid(),
                Name = child.Name,
                PreconditionSteps = new List<Step>(),
                PostconditionSteps = new List<Step>(),
                Sections = ConvertChildSection(child.Id, spiraFolders)
            };

            sections.Add(section);
            _sectionMap.Add(child.Id, section.Id);
        }

        return sections;
    }
}
