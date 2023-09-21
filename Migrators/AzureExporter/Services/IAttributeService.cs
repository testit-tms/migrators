using Attribute = Models.Attribute;

namespace AzureExporter.Services;

public interface IAttributeService
{
    Task<List<Attribute>> GetCustomAttributes(Guid projectId);
}
