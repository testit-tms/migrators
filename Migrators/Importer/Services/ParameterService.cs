using Importer.Client;
using Importer.Models;
using Microsoft.Extensions.Logging;
using Models;

namespace Importer.Services;

public class ParameterService : IParameterService
{
    private readonly ILogger<ParameterService> _logger;
    private readonly IClient _client;

    public ParameterService(ILogger<ParameterService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<List<TmsParameter>> CreateParameters(IEnumerable<Parameter> parameters)
    {
        _logger.LogInformation("Creating parameters");

        var ids = new List<TmsParameter>();

        foreach (var parameter in parameters)
        {
            var tmsParameters = await _client.GetParameter(parameter.Name);

            var existParameter = tmsParameters.FirstOrDefault(p => p.Value == parameter.Value);

            if (existParameter is not null)
            {
                _logger.LogDebug("Parameter {Name} already exist", parameter.Name);

                ids.Add(existParameter);
                continue;
            }

            var newParameter = await _client.CreateParameter(parameter);
            ids.Add(newParameter);
        }

        return ids;
    }
}
