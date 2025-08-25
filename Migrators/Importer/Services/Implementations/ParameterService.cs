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

            try
            {
                var newParameter = await clientAdapter.CreateParameter(parameter);
                ids.Add(newParameter);
            }
            // cannot create equals - exists, but not with the same value
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create parameter {Name}", parameter.Name);

                var nAParameter = tmsParameters.FirstOrDefault(p => p.Value == "N/A");
                var nothingParameter = tmsParameters.FirstOrDefault(p => p.Value == string.Empty);

                if (nAParameter is not null)
                {
                    logger.LogDebug("Parameter {Name} already exist with N/A", parameter.Name);

                    ids.Add(nAParameter);
                    continue;
                }

                if (nothingParameter is not null)
                {
                    logger.LogDebug("Parameter {Name} already exist with empty string", parameter.Name);

                    ids.Add(nothingParameter);
                    continue;
                }
            }
        }
        return ids;
    }
}
