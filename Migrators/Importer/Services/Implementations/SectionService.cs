using Importer.Client;
using Microsoft.Extensions.Logging;
using Models;

namespace Importer.Services.Implementations;

internal class SectionService(ILogger<SectionService> logger, IClientAdapter clientAdapter)
    : ISectionService
{
    private readonly Dictionary<Guid, Guid> _sectionsMap = new();

    public async Task<Dictionary<Guid, Guid>> ImportSections(Guid projectId, IEnumerable<Section> sections)
    {
        logger.LogInformation("Importing sections");

        var rootSectionId = await clientAdapter.GetRootSectionId(projectId);

        foreach (var section in sections) await ImportSection(projectId, rootSectionId, section);

        return _sectionsMap;
    }

    private async Task ImportSection(Guid projectId, Guid parentSectionId, Section section)
    {
        logger.LogDebug("Importing section {Name} to parent section {Id}",
            section.Name,
            parentSectionId);

        var sectionId = await clientAdapter.ImportSection(projectId, parentSectionId, section);
        _sectionsMap.Add(section.Id, sectionId);

        foreach (var sectionSection in section.Sections) await ImportSection(projectId, sectionId, sectionSection);

        logger.LogDebug("Imported section {Name} to parent section {Id}",
            section.Name,
            parentSectionId);
    }
}