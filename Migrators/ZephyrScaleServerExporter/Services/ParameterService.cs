using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models;
using Microsoft.Extensions.Logging;
using Models;

namespace ZephyrScaleServerExporter.Services;

public class ParameterService : IParameterService
{
    private readonly ILogger<ParameterService> _logger;
    private readonly IClient _client;

    public ParameterService(ILogger<ParameterService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<List<Iteration>> ConvertParameters(string testCaseKey)
    {
        _logger.LogInformation("Converting parameters");

        var parametersData = await _client.GetParametersByTestCaseKey(testCaseKey);

        switch (parametersData.Type)
        {
            case ZephyrParameterType.TEST_DATA:
                return ConvertParametersWithTestDataType(parametersData.TestData);
            case ZephyrParameterType.PARAMETER:
                return ConvertParametersWithParameterType(parametersData.Parameters);
            default:
                return new List<Iteration>();
        }
    }

    public List<Iteration> MergeIterations(List<Iteration> mainIterations, List<Iteration> subIterations)
    {
        _logger.LogInformation("Merging parameters:\nMain: {@MainParameters}\n Sub: {@SubParameters}",
            mainIterations, subIterations);

        foreach (var mainIteration in mainIterations)
        {
            foreach (var subIteration in subIterations)
            {
                var nonconflictingIterationParameters = subIteration.Parameters.Where(
                    subp => mainIteration.Parameters.FirstOrDefault(
                        mainp => subp.Name == mainp.Name) == null);

                mainIteration.Parameters.AddRange(nonconflictingIterationParameters);
            }
        }

        _logger.LogInformation("Merged parameters: {@Parameters}", mainIterations);

        return mainIterations;
    }

    private List<Iteration> ConvertParametersWithTestDataType(List<Dictionary<string, ZephyrDataParameter>> ZephyrTestData)
    {
        var iterations = new List<Iteration>();

        foreach (var zephyrDataParameters in ZephyrTestData)
        {
            var iteration = new Iteration
            {
                Parameters = new List<Parameter>()
            };

            foreach (var name in zephyrDataParameters.Keys)
            {
                iteration.Parameters.Add(
                    new Parameter
                    {
                        Name = name,
                        Value = zephyrDataParameters[name].Value
                    });
            }

            iterations.Add(iteration);
        }

        _logger.LogInformation("Converted parameters: {@Parameters}", iterations);

        return iterations;
    }

    private List<Iteration> ConvertParametersWithParameterType(List<ZephyrParameter> zephyrParameters)
    {
        var parameters = new List<Parameter>();


        foreach (var zephyrParameter in zephyrParameters)
        {
            parameters.Add(
                new Parameter
                {
                    Name = zephyrParameter.Name,
                    Value = zephyrParameter.Value
                });
        }

        _logger.LogInformation("Converted parameters: {@Parameters}", parameters);

        return new List<Iteration>
        {
            new Iteration
            {
                Parameters = parameters
            }
        };
    }
}
