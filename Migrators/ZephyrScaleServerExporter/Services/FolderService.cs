using Microsoft.Extensions.Logging;
using Models;
using ZephyrScaleServerExporter.Models;
using Constants = ZephyrScaleServerExporter.Models.Constants;

namespace ZephyrScaleServerExporter.Services;

public class FolderService : IFolderService
{
    private readonly ILogger<FolderService> _logger;
    private readonly Dictionary<string, Guid> _sectionMap;
    private readonly Dictionary<string, Section> _allSections;

    public FolderService(ILogger<FolderService> logger)
    {
        _logger = logger;
        _sectionMap = new Dictionary<string, Guid>();
        _allSections = new Dictionary<string, Section>();
    }

    public async Task<SectionData> ConvertSections(string projectName)
    {
        _logger.LogDebug("Creating main section with name {@Name}", projectName);

        var section = new Section
        {
            Id = Guid.NewGuid(),
            Name = projectName,
            Sections = new List<Section>(),
            PostconditionSteps = new List<Step>(),
            PreconditionSteps = new List<Step>()
        };

        _sectionMap.Add(Constants.MainFolderKey, section.Id);
        _allSections.Add(Constants.MainFolderKey, section);

        var sectionData = new SectionData
        {
            MainSection = section,
            SectionMap = _sectionMap,
            AllSections = _allSections,
        };

        _logger.LogDebug("Sections: {@SectionData}", sectionData);

        return sectionData;
    }
}
