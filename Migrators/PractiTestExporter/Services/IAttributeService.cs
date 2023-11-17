using PractiTestExporter.Models;

namespace PractiTestExporter.Services;

public interface IAttributeService
{
    Task<AttributeData> ConvertCustomAttributes();
}
