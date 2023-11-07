using TestCollabExporter.Models;

namespace TestCollabExporter.Services;

public interface ISectionService
{
    Task<SectionData> ConvertSection(int projectId);
}
