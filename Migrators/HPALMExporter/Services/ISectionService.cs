using HPALMExporter.Models;

namespace HPALMExporter.Services;

public interface ISectionService
{
    Task<SectionData> ConvertSections();
}
