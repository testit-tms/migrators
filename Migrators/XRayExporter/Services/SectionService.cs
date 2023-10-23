using Microsoft.Extensions.Logging;
using Models;
using XRayExporter.Client;
using XRayExporter.Models;
using Step = Models.Step;

namespace XRayExporter.Services;

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

    public async Task<SectionData> ConvertSections()
    {
        _logger.LogInformation("Converting sections");

        var folders = await _client.GetFolders();

        var sections = ConvertSections(folders);

        var sectionData = new SectionData
        {
            Sections = sections,
            SectionMap = _sectionMap
        };

        _logger.LogInformation("Sections converted");

        return sectionData;
    }

    private List<Section> ConvertSections(IEnumerable<XrayFolder> folders)
    {
        var xrayFolders = folders.ToList();

        if (!xrayFolders.Any())
        {
            return new List<Section>();
        }

        var sections = new List<Section>();

        foreach (var child in xrayFolders)
        {
            _logger.LogDebug("Converting folder {@Folder}", child);

            var section = new Section
            {
                Id = Guid.NewGuid(),
                Name = child.Name,
                Sections = ConvertSections(xrayFolders),
                PostconditionSteps = new List<Step>(),
                PreconditionSteps = new List<Step>()
            };

            sections.Add(section);
            _sectionMap.Add(child.Id, section.Id);
        }

        return sections;
    }
}
