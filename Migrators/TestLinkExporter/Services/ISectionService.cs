using Models;
using TestLinkExporter.Models.Suite;

namespace TestLinkExporter.Services;

public interface ISectionService
{
    SectionData ConvertSections(int projectId);
}
