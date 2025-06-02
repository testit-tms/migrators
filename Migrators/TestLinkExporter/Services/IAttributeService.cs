using Attribute = Models.Attribute;

namespace TestLinkExporter.Services;

public interface IAttributeService
{
    List<Attribute> GetCustomAttributes();
}
