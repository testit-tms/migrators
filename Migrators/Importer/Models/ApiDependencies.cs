using TestIT.ApiClient.Client;

namespace Importer.Models;

public class ApiDependencies
{
    public TimeSpan Timeout { get; set; }
    public HttpClientHandler Handler { get; set; }
    public Configuration Config { get; set; }
}
