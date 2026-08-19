using TestRailExporter.Models.Commons;

namespace TestRailExporter.Services;

public interface IAttributeService
{
    Task<AttributeData> ConvertAttributes();
}
