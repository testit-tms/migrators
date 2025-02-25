using TestRailExporter.Models;

namespace TestRailExporter.Services;

public interface ISectionService
{
    Task<SectionInfo> ConvertSections(int projectId);
}
