using TestCollabExporter.Models;

namespace TestCollabExporter.Services;

public interface IAttributeService
{
    Task<AttributeData> ConvertAttributes(int companyId);
}
