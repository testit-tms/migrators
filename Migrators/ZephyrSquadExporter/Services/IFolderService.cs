using ZephyrSquadExporter.Models;

namespace ZephyrSquadExporter.Services;

public interface IFolderService
{
    Task<SectionData> GetSections();
}
