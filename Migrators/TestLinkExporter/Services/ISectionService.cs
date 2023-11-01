using Models;
using TestLinkExporter.Models;

namespace TestLinkExporter.Services;

public interface ISectionService
{
    SectionData ConvertSections(int projectId);
}
