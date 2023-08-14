using AllureExporter.Client;
using Microsoft.Extensions.Logging;

namespace AllureExporter;

public class App
{
    private readonly ILogger<App> _logger;
    private readonly IClient _client;

    public App(ILogger<App> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public void Run(string[] args)
    {
        _logger.LogInformation("Starting application");

        var id = _client.GetProjectId().Result;
        _logger.LogInformation("Project id is {Id}", id);

        _logger.LogInformation("Ending application");
    }
}
