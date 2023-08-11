using Importer.Models;
using Attribute = Models.Attribute;

namespace Importer.Services;

public interface IAttributeService
{
    Task<Dictionary<Guid, TmsAttribute>> ImportAttributes(IEnumerable<Attribute> attributes);
}
