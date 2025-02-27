using TestRailExporter.Models.Commons;

namespace TestRailExporter.Services;

public interface ISectionService
{
    Task<SectionInfo> ConvertSections(int projectId);
}
