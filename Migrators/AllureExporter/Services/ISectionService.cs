using AllureExporter.Models;

namespace AllureExporter.Services;

public interface ISectionService
{
    Task<SectionInfo> ConvertSection(int projectId);
}
