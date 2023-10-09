using ZephyrScaleExporter.Models;

namespace ZephyrScaleExporter.Services;

public interface IFolderService
{
    Task<SectionData> ConvertSections();
}
