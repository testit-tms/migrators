using Attribute = Models.Attribute;

namespace HPALMExporter.Services;

public interface IAttributeService
{
    Task<List<Attribute>> ConvertAttributes();
}
