using TestIT.ApiClient.Client;

namespace Importer.Client;

public interface IApiConfigurationFactory
{
    Configuration Create();
}