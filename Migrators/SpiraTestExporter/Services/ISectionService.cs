using SpiraTestExporter.Models;

namespace SpiraTestExporter.Services;

public interface ISectionService
{
    Task<SectionData> GetSections(int projectId);
}
