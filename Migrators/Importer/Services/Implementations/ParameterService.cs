using Importer.Client;
using Importer.Models;
using Microsoft.Extensions.Logging;
using Models;

namespace Importer.Services.Implementations;

internal class ParameterService(ILogger<ParameterService> logger, IClientAdapter clientAdapter)
    : IParameterService
{
    public async Task<List<TmsParameter>> CreateParameters(IEnumerable<Parameter> parameters)
    {
        logger.LogInformation("Creating parameters");

        var ids = new List<TmsParameter>();

        foreach (var parameter in parameters)
        {
            var tmsParameters = await clientAdapter.GetParameter(parameter.Name);

            var existParameter = tmsParameters.FirstOrDefault(p => p.Value == parameter.Value);

            if (existParameter is not null)
            {
                logger.LogDebug("Parameter {Name} already exist", parameter.Name);

                ids.Add(existParameter);
                continue;
            }

            var newParameter = await clientAdapter.CreateParameter(parameter);
            ids.Add(newParameter);
        }

        return ids;
    }
}