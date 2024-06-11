using ZephyrScaleServerExporter.Models;

namespace ZephyrScaleServerExporter.Services;

public interface IFolderService
{
    Task<SectionData> ConvertSections(string projectName);
}
