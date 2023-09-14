using Attribute = Models.Attribute;

namespace AllureExporter.Services;

public interface IAttributeService
{
    Task<List<Attribute>> GetCustomAttributes(int projectId);
}
