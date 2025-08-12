using ZephyrScaleServerExporter.Models.Attributes;

namespace ZephyrScaleServerExporter.Services;

public interface IAttributeService
{
    Task<AttributeData> ConvertAttributes(string projectId);
}
