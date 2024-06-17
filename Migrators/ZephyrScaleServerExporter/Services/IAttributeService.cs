using ZephyrScaleServerExporter.Models;

namespace ZephyrScaleServerExporter.Services;

public interface IAttributeService
{
    Task<AttributeData> ConvertAttributes(string projectKey);
}
