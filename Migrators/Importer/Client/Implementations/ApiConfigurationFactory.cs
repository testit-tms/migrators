using Importer.Models;
using Microsoft.Extensions.Options;
using TestIT.ApiClient.Client;

namespace Importer.Client.Implementations;

public class ApiConfigurationFactory(
    IOptions<AppConfig> config)
    : IApiConfigurationFactory
{
    public Configuration Create()
    {
        var configV = config.Value;
        var url = configV.Tms.Url;
        var token = configV.Tms.PrivateToken;
        var timeout = TimeSpan.FromSeconds(configV.Tms.Timeout);

        var cfg = new Configuration { BasePath = url.TrimEnd('/') };
        cfg.AddApiKeyPrefix("Authorization", "PrivateToken");
        cfg.AddApiKey("Authorization", token);
        cfg.Timeout = (int)timeout.TotalMilliseconds;

        return cfg;
    }
}