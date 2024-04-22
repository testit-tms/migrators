using ZephyrSquadServerExporter.Models;

namespace ZephyrSquadServerExporter.Services;

public interface IFolderService
{
    Task<SectionData> GetSections(List<JiraProjectVersion> versions, string projectId);
}
