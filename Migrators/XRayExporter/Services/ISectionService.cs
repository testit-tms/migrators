using XRayExporter.Models;

namespace XRayExporter.Services;

public interface ISectionService
{
    Task<SectionData> ConvertSections();
}
