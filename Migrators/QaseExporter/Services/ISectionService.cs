using Models;
using QaseExporter.Models;

namespace QaseExporter.Services;

public interface ISectionService
{
    Task<SectionData> ConvertSections();
}
