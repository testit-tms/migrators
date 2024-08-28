using QaseExporter.Client;
using Microsoft.Extensions.Logging;
using Models;

namespace QaseExporter.Services;

public class ParameterService : IParameterService
{
    private readonly ILogger<ParameterService> _logger;

    public ParameterService(ILogger<ParameterService> logger, IClient client)
    {
        _logger = logger;
    }

    //TODO: need to redo it easier
    public List<Iteration> ConvertParameters(Dictionary<string, List<string>> qaseParameters)
    {
        _logger.LogInformation("Converting parameters");

        var iterations = new List<Iteration>();

        if (qaseParameters.Keys.Any())
        {
            var qaseParameterName = qaseParameters.Keys.First();
            var otherParametersIterations = ConvertParameters(
                qaseParameters.Skip(1)
                      .Take(qaseParameters.Count)
                      .ToDictionary(pair => pair.Key, pair => pair.Value));

            foreach (var parameterValue in qaseParameters[qaseParameterName])
            {
                if (!otherParametersIterations.Any())
                {
                    iterations.Add(new Iteration
                    {
                        Parameters = new List<Parameter>()
                        {
                            new Parameter
                            {
                                Name = qaseParameterName,
                                Value = parameterValue
                            }
                        }
                    });

                    continue;
                }

                foreach (var iteration in otherParametersIterations)
                {
                    var updatedIteration = new Iteration()
                    {
                        Parameters = iteration.Parameters.Select(p => p).ToList()
                    };

                    updatedIteration.Parameters.Add(
                        new Parameter
                        {
                            Name = qaseParameterName,
                            Value = parameterValue
                        }
                    );

                    iterations.Add(updatedIteration);
                }
            }
        }

        _logger.LogInformation("Converted parameters: {@Parameters}", iterations);

        return iterations;
    }
}
