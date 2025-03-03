using AllureExporter.Models.Project;

namespace AllureExporter.Services;

public interface ISectionService
{
    Task<SectionInfo> ConvertSection(long projectId);
}
