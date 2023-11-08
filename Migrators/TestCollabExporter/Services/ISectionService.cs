using TestCollabExporter.Models;

namespace TestCollabExporter.Services;

public interface ISectionService
{
    Task<SectionData> ConvertSections(int projectId);
}
