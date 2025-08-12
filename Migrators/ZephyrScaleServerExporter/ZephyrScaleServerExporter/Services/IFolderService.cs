using ZephyrScaleServerExporter.Models.Common;

namespace ZephyrScaleServerExporter.Services;

public interface IFolderService
{
    SectionData ConvertSections(string projectName);
}
