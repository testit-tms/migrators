using Microsoft.Extensions.Logging;
using Models;
using ZephyrScaleExporter.Client;
using ZephyrScaleExporter.Models;

namespace ZephyrScaleExporter.Services;

public class FolderService : IFolderService
{
    private readonly ILogger<FolderService> _logger;
    private readonly IClient _client;
    private readonly Dictionary<int, Guid> _sectionMap;

    public FolderService(ILogger<FolderService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
        _sectionMap = new Dictionary<int, Guid>();
    }

    public async Task<SectionData> ConvertSections()
    {
        var folders = await _client.GetFolders();
        var sections = new List<Section>();

        foreach (var folder in folders.Where(f => f.ParentId == null))
        {
            _logger.LogDebug("Converting folder {@Folder}", folder);

            var section = new Section
            {
                Id = Guid.NewGuid(),
                Name = folder.Name,
                Sections = GetChildrenSections(folder.Id, folders),
                PostconditionSteps = new List<Step>(),
                PreconditionSteps = new List<Step>()
            };

            sections.Add(section);
            _sectionMap.Add(folder.Id, section.Id);
        }

        var sectionData = new SectionData
        {
            Sections = sections,
            SectionMap = _sectionMap
        };

        _logger.LogDebug("Sections: {@SectionData}", sectionData);

        return sectionData;
    }

    private List<Section> GetChildrenSections(int? id, IEnumerable<ZephyrFolder> folders)
    {
        if (id == null)
        {
            return new List<Section>();
        }

        var zephyrFolders = folders.ToList();
        var children = zephyrFolders.Where(f => f.ParentId == id).ToList();

        var sections = new List<Section>();

        foreach (var zephyrFolder in children)
        {
            var section = new Section
            {
                Id = Guid.NewGuid(),
                Name = zephyrFolder.Name,
                Sections = GetChildrenSections(zephyrFolder.Id, zephyrFolders),
                PostconditionSteps = new List<Step>(),
                PreconditionSteps = new List<Step>()
            };

            sections.Add(section);
            _sectionMap.Add(zephyrFolder.Id, section.Id);
        }

        return sections;
    }
}
