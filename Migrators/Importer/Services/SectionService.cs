using Importer.Client;
using Microsoft.Extensions.Logging;
using Models;

namespace Importer.Services;

public class SectionService : ISectionService
{
    private readonly ILogger<SectionService> _logger;
    private readonly IClient _client;
    private readonly Dictionary<Guid, Guid> _sectionsMap = new();

    public SectionService(ILogger<SectionService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<Dictionary<Guid, Guid>> ImportSections(Guid projectId, IEnumerable<Section> sections)
    {
        _logger.LogInformation("Importing sections");

        var rootSectionId = await _client.GetRootSectionId(projectId);

        foreach (var section in sections)
        {
            await ImportSection(projectId, rootSectionId, section);
        }

        return _sectionsMap;
    }

    private async Task ImportSection(Guid projectId, Guid parentSectionId, Section section)
    {
        _logger.LogDebug("Importing section {Name} to parent section {Id}",
            section.Name,
            parentSectionId);

        var sectionId = await _client.ImportSection(projectId, parentSectionId, section);
        _sectionsMap.Add(section.Id, sectionId);

        foreach (var sectionSection in section.Sections)
        {
            await ImportSection(projectId, sectionId, sectionSection);
        }

        _logger.LogDebug("Imported section {Name} to parent section {Id}",
            section.Name,
            parentSectionId);
    }
}
