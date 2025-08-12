using Models;
using ZephyrScaleServerExporter.Models.Common;
using Constants = ZephyrScaleServerExporter.Models.Common.Constants;

namespace ZephyrScaleServerExporter.Services.Implementations;

internal class FolderService(
    IDetailedLogService detailedLogService) 
    : IFolderService
{
    private readonly Dictionary<string, Guid> _sectionMap = new();
    private readonly Dictionary<string, Section> _allSections = new();

    public SectionData ConvertSections(string projectName)
    {
        detailedLogService.LogDebug("Creating main section with name {@Name}", projectName);

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

        detailedLogService.LogDebug("Sections: {@SectionData}", sectionData);

        return sectionData;
    }
}
