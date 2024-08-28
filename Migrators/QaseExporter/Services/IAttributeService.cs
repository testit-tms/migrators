using QaseExporter.Models;

namespace QaseExporter.Services;

public interface IAttributeService
{
    Task<AttributeData> ConvertAttributes();
}
