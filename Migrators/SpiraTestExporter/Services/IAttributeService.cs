using SpiraTestExporter.Models;

namespace SpiraTestExporter.Services;

public interface IAttributeService
{
    Task<AttributeData> GetAttributes(int projectTemplateId);
}
