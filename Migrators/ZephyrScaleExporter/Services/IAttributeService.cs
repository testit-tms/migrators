using ZephyrScaleExporter.Models;

namespace ZephyrScaleExporter.Services;

public interface IAttributeService
{
    Task<AttributeData> ConvertAttributes();
}
